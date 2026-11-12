using UnityEngine;

namespace Snowglow
{
    // Physics support for compressible snow; the .5 m visual/volume grid is untouched.
    internal sealed class SnowSupportSurface
    {
        internal const int Step = 2, Side = SnowField.Chunk / Step + 1;
        const float SoftDepth = .45f, Compression = .65f;
        static readonly int[] Weights = { 1, 3, 5, 5, 3, 1 };
        static readonly int[] Triangles = MakeTriangles();
        readonly float[] heights = new float[Side * Side];
        readonly Vector3[] vertices = new Vector3[Side * Side];
        internal Mesh mesh;
        internal bool pending;
        float updated;

        internal static float Sample(SnowVolumeGrid grid, int x, int z)
        {
            float sum = 0;
            for (int dz = -3; dz < 3; dz++)
                for (int dx = -3; dx < 3; dx++)
                {
                    int sx = x + dx, sz = z + dz;
                    if (sx < 0 || sz < 0 || sx >= SnowVolumeGrid.Size || sz >= SnowVolumeGrid.Size) continue;
                    float depth = grid.depth[sz * SnowVolumeGrid.Size + sx];
                    float compacted = Mathf.Max(0, depth - SoftDepth) * Compression;
                    // Visual-review tuning: halve the gap to filtered visible snow.
                    sum += Mathf.Lerp(compacted, depth, .5f) * Weights[dx + 3] * Weights[dz + 3];
                }
            return sum / 324f;
        }

        internal bool Update(SnowVolumeGrid grid, int chunkX, int chunkZ, MeshCollider collider, bool immediate)
        {
            float dt = Mathf.Min(.1f, Mathf.Max(0, Time.time - updated));
            updated = Time.time;
            bool changed = mesh == null;
            pending = false;
            float peak = 0;
            for (int z = 0; z < Side; z++)
                for (int x = 0; x < Side; x++)
                {
                    int i = z * Side + x, gx = chunkX * SnowField.Chunk + x * Step, gz = chunkZ * SnowField.Chunk + z * Step;
                    float target = Sample(grid, gx, gz);
                    // Never instantaneously grow a new hard surface into a wheel/chassis.
                    float next = immediate ? target : Mathf.MoveTowards(heights[i], target, (target > heights[i] ? .35f : 1.2f) * dt);
                    pending |= Mathf.Abs(next - target) > .0001f;
                    changed |= Mathf.Abs(next - heights[i]) > .00001f;
                    heights[i] = next;
                    peak = Mathf.Max(peak, next);
                    vertices[i] = new Vector3(SnowVolumeGrid.Origin + gx * SnowVolumeGrid.Cell, SnowField.Ground + next, SnowVolumeGrid.Origin + gz * SnowVolumeGrid.Cell);
                }
            if (!changed) return pending;
            if (mesh == null && peak <= .002f) return pending;
            bool created = mesh == null;
            if (created) mesh = new Mesh { name = "Compacted snow support " + chunkX + "," + chunkZ };
            collider.sharedMesh = null;
            mesh.vertices = vertices;
            if (created) mesh.triangles = Triangles;
            mesh.RecalculateBounds();
            collider.enabled = peak > .002f;
            if (collider.enabled) collider.sharedMesh = mesh;
            return pending;
        }
        static int[] MakeTriangles()
        {
            var result = new int[(Side - 1) * (Side - 1) * 6]; int n = 0;
            for (int z = 0; z < Side - 1; z++) for (int x = 0; x < Side - 1; x++)
            {
                int a = z * Side + x;
                result[n++] = a; result[n++] = a + Side; result[n++] = a + 1;
                result[n++] = a + 1; result[n++] = a + Side; result[n++] = a + Side + 1;
            }
            return result;
        }
    }
}
