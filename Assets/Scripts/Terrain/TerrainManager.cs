using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using ThreadPriority = UnityEngine.ThreadPriority;

namespace Minecraft
{
    public class TerrainManager : MonoBehaviour
    {
        public static TerrainManager Instance { get; private set; }

        public string Seed
        {
            get => seed;
            set => seed = value;
        }

        public TerrainConfig TerrainConfig => config;
        public float RayOffset => rayOffset;

        [SerializeField] private string seed;
        [SerializeField] private TerrainGeneratorBase terrainGenerator;
        [SerializeField] private TerrainChunkMesherBase chunkMesher;
        [SerializeField] private ThreadPriority chunkGeneratePriority = ThreadPriority.Low;
        [SerializeField] private TerrainChunksPool chunksPool;
        [SerializeField] private TerrainConfig config;
        [SerializeField] private uint renderDistance = 12U;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private GameObject playerPrefab;
        [Header("Misc")] [SerializeField] private Font minecraftFont;

        [FormerlySerializedAs("_rayOffset"),
         SerializeField]
        private float rayOffset = 0.01f;

        private Transform _player;
        private uint _prevRenderDistance;
        private Vector3Int _playerChunk = Vector3Int.one * int.MinValue;
        private readonly HashSet<Vector3Int> _currentChunks = new();
        private bool _updatingTerrain;
        private bool _forceUpdate;

        private TerrainChunkGenerator _chunkGenerator;

        private void Awake()
        {
            Instance = this;

            if (!int.TryParse(seed, out int numSeed))
                numSeed = seed.GetHashCode();

            Debug.Assert(terrainGenerator != null);
            terrainGenerator.SetSeed(numSeed);

            _player = playerPrefab.transform;
            _playerChunk = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            _chunkGenerator = new TerrainChunkGenerator(chunksPool, config);

            // Sometimes there's an error but no exception thrown due to how Unity and Tasks work
            // This chunk allows us to see these errors
            TerrainChunk debugChunk = chunksPool.Instantiate(Vector3Int.zero, transform);
            debugChunk.name = "[DEBUG] CHUNK";
            debugChunk.Setup(Vector3Int.zero, config, false);
            terrainGenerator.GenerateBlocksFor(debugChunk);
            RegenerateChunkMesh(debugChunk);

            chunksPool.DisposeAll();
        }

        // Start is called before the first frame update
        private async void Start()
        {
            _forceUpdate = true;
            await UpdateTerrain().ContinueWith(_ => Debug.Log("First load finished"));
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                var pchunk = GetChunkIndexAt(_player.position);
                pchunk.y = 0;
                var scaledChunkSize = (Vector3)config.chunkSize * config.blockSize;
                Gizmos.DrawWireCube(Vector3.Scale(pchunk, scaledChunkSize) + scaledChunkSize / 2 - 0.5f * Vector3.one,
                    scaledChunkSize);
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new();
            style.normal.textColor = Color.white;
            style.font = minecraftFont;
            GUI.Label(new Rect(5f, Screen.height - 25f, 200f, 25f), $"Player.Y: {_player.position.y}", style);
        }

        private async void Update()
        {
            if (_prevRenderDistance != renderDistance && renderDistance != 0)
            {
                chunksPool.Deactivate(_currentChunks.Where((i => i != _playerChunk)));
                _currentChunks.Clear();
                _forceUpdate = true;
                _updatingTerrain = false;
                _prevRenderDistance = renderDistance;
            }

            Application.backgroundLoadingPriority = chunkGeneratePriority;
            await UpdateTerrain();
            Application.backgroundLoadingPriority = ThreadPriority.Normal;
        }

        private async Task UpdateTerrain()
        {
            if (_updatingTerrain) return;
            var newPlayerChunk = GetChunkIndexAt(_player.position);

            if (_forceUpdate || _playerChunk != newPlayerChunk)
            {
                var newCurrentChunks = ChunksAroundChunk(_playerChunk);
                var chunksToDestroy = _currentChunks.Except(newCurrentChunks);
                var chunksToCreate = newCurrentChunks.Except(_currentChunks);
                chunksPool.Deactivate(chunksToDestroy);

                _updatingTerrain = true;

                await GenerateChunks(chunksToCreate).ContinueWith(_ =>
                {
                    _currentChunks.Clear();
                    _currentChunks.UnionWith(newCurrentChunks);
                    _playerChunk = newPlayerChunk;
                    _updatingTerrain = false;
                    _forceUpdate = false;
                });
            }
        }
        private IEnumerable<Vector3Int> ChunksAroundChunk(Vector3Int chunkIndex)
        {
            for (int i = 1; i <= (int)renderDistance; i++)
            {
                for (int x = chunkIndex.x - i; x <= chunkIndex.x + i; x++)
                {
                    for (int z = chunkIndex.z - i; z <= chunkIndex.z + i; z++)
                    {
                        yield return new Vector3Int(x, 0, z);
                    }
                }
            }
        }

        private async Task GenerateChunks(IEnumerable<Vector3Int> chunksToCreate)
        {
            foreach (var chunkIndex in chunksToCreate)
            {
                var newChunk = _chunkGenerator.InstantiateAndSetup(chunkIndex, transform);
                terrainGenerator.GenerateBlocksFor(newChunk);
                // TODO: Neighbour chunks
                chunkMesher.GenerateMeshFor(newChunk);
                await Task.Yield();
            }
        }

        public PointOnTerrainMesh RaycastTerrainMesh(Ray ray, float offset, float maxDistance = 5f)
        {
            PointOnTerrainMesh result = null;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                var point = hit.point + ray.direction * offset;
                result = new PointOnTerrainMesh(point, hit.normal, ray);
            }

            return result;
        }

        public TerrainBlock? AddBlock(Ray ray, VoxelType blockType)
        {
            TerrainBlock? block = null;
            var pointOnTerrain = RaycastTerrainMesh(ray, -rayOffset)?.Point;

            if (pointOnTerrain.HasValue)
            {
                block = GetBlockAt(pointOnTerrain.Value, out TerrainChunk chunk);

                SetBlockTypeAt(chunk, block.Value.index, blockType);
                RegenerateChunkMesh(chunk);
            }

            return block;
        }

        public TerrainBlock? RemoveBlock(Ray ray)
        {
            TerrainBlock? block = null;
            var pointOnTerrain = RaycastTerrainMesh(ray, rayOffset)?.Point;

            if (!pointOnTerrain.HasValue) return null;
            block = GetBlockAt(pointOnTerrain.Value, out TerrainChunk chunk);

            SetBlockTypeAt(chunk, block.Value.index, VoxelType.Air);
            RegenerateChunkMesh(chunk);

            return block;
        }

        public TerrainBlock GetBlockAt(Vector3 worldPoint, out TerrainChunk chunk)
        {
            var chunkIndex = GetChunkIndexAt(worldPoint);
            chunk = chunksPool.GetChunk(chunkIndex);

            if (!chunk) chunk = _chunkGenerator.GetOrGenerateChunk(chunkIndex, transform);

            return chunk.GetBlock(
                Vector3Int.FloorToInt(worldPoint - (chunk.Index * chunk.Size) + 0.5f * Vector3.one)
            ).GetValueOrDefault();
        }

        public TerrainBlock GetBlockAt(Vector3 worldPoint)
        {
            return GetBlockAt(worldPoint, out _);
        }

        public void SetBlockTypeAt(TerrainChunk chunk, Vector3Int localBlockIndex, VoxelType type,
            bool regenerateMesh = false)
        {
            Debug.Assert(chunk, "Chunk must not be null");
            chunk.SetBlockType(localBlockIndex, type);

            if (!regenerateMesh) return;
            RegenerateChunkMesh(chunk);
        }

        public Vector3Int GetChunkIndexAt(Vector3 pointInWorld)
        {
            return Vector3Int.FloorToInt(new Vector3(
                pointInWorld.x / config.chunkSize.x,
                pointInWorld.y / config.chunkSize.y,
                pointInWorld.z / config.chunkSize.z
            ));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegenerateChunkMesh(TerrainChunk chunk) => chunkMesher.GenerateMeshFor(chunk);
    }
}