using System.Threading;
using PicaVoxel;    
using UnityEngine;
using System.Collections;

public class ShmupPlayfield : MonoBehaviour
{
    enum Tiles
    {
        Ground = 0,
        Tree1=1,
        Tree2=2,
        Tree3=3
    }

    // We're using a volume to define tiles to copy to the playfield.
    // Each animation frame in this volume is treated as a tile, referenced by the Tiles enum
    public Volume Tileset;

    // An array to reference each tile in the Playfield (12x6)
    private const int X_TILES = 20;
    private const int Y_TILES = 8;
    private Volume[,] tiles = new Volume[X_TILES,Y_TILES];

    private float scrollSpeed =5f;
    private float scrolledAmount;

    private Color wallColor = new Color32(154,114,45,255);
    private int wallDepth = 16;

    private ParticleSystem.Particle[] parts;

	void Start () {
	    
        // Initialise the playfield
	    for (int x = 0; x < X_TILES; x++)
	    {
	        for (int y = 0; y < Y_TILES; y++)
	        {
	            // Let's get a reference to the tile in the playfield
	            // We'll do this by name, although there's probably better ways
	            tiles[x, y] = transform.Find("Playfield (" + x + "," + y + ",0)").GetComponent<Volume>();
	        }
            GenerateNewColumn(x);
	    }

        foreach (var volume in tiles)
            volume.UpdateAllChunks();

	    Physics.gravity = new Vector3(0,0,10);

	}
	
	void FixedUpdate () {

        // Scroll the tiles!
	    for(int x=0;x<X_TILES;x++)
	        for (int y = 0; y < Y_TILES; y++)
	        {
	            tiles[x,y].transform.Translate(-Time.deltaTime * scrollSpeed,0f,0f);
	        }

	    scrolledAmount += Time.deltaTime * scrollSpeed;

        // This is the clever part! When we've scrolled far enough, move the leftmost colum of tiles over to the right, and re-initialise
        if (scrolledAmount >= 16f * tiles[0, 0].VoxelSize)
	    {
	        scrolledAmount = 0f;
            for(int y=0;y<Y_TILES;y++)
                tiles[0,y].transform.Translate(X_TILES*16*tiles[0,y].VoxelSize,0f,0f);

            // And we also have to scroll the reference array
	        Volume[] tempRefs = new Volume[Y_TILES];

	        for (int y = 0; y < Y_TILES; y++)
	            tempRefs[y] = tiles[0, y];

            for(int x=0;x<X_TILES-1;x++)
                for (int y = 0; y < Y_TILES; y++)
                    tiles[x, y] = tiles[x + 1, y];

            for (int y = 0; y < Y_TILES; y++)
                tiles[X_TILES-1, y] = tempRefs[y];

	        GenerateNewColumn(X_TILES-1);
	    }

        // Scroll the particles as well
        if (parts == null) parts = new ParticleSystem.Particle[VoxelParticleSystem.Instance.System.main.maxParticles];
        int numParts = VoxelParticleSystem.Instance.System.GetParticles(parts);
        if (numParts > 0)
        {
            for (int p = 0; p < numParts; p++)
            {
                parts[p].position += new Vector3(-Time.deltaTime * scrollSpeed,0,0);
            }
        }
        VoxelParticleSystem.Instance.System.SetParticles(parts, numParts);

	}

    // Generate a new column on the right when we scroll off the left
    private void GenerateNewColumn(int tileX)
    {
        // Generate some basic tiles
	    for (int y = 0; y < Y_TILES; y++)
            GenerateBasicTile(tiles[tileX, y], tileX, y);

        // Now we do the top and bottom walls.
        int yTile = 0;
        int yPos = 0;
        for (int x = 0; x < 16; x++)
        {
            yPos = 0;
            yTile = 0;
            for (int y = 0; y < wallDepth; y++)
            {

                for (int z = 0; z < 16; z++)
                {
                    tiles[tileX, yTile].Frames[0].Voxels[x + 16 * (yPos + 16 * z)] = new Voxel()
                    {
                        State = VoxelState.Active,
                        Color = wallColor,
                        Value = 0
                    };

                    tiles[tileX, (Y_TILES - 1) - yTile].Frames[0].Voxels[x + 16 * ((15 - yPos) + 16 * z)] = new Voxel()
                    {
                        State = VoxelState.Active,
                        Color = wallColor,
                        Value = 0
                    };
                }

                yPos++;
                if (yPos >= 16)
                {
                    yTile++;
                    yPos = 0;
                }

                
            }

            // Randomly change the wall depth
            int wallChange = Random.Range(0, 5);
            if (wallChange == 0 && wallDepth > 8) wallDepth--;
            if (wallChange == 4 && wallDepth < 31) wallDepth++;

            
            
        }

        for (int y = 0; y < Y_TILES; y++)
            tiles[tileX, y].UpdateAllChunksNextFrame();
    }

    // Procedurally generate a tile
    void GenerateBasicTile(Volume tile, int tileX, int tileY)
    {
        // Clear the tile
        tile.Frames[0].Voxels = new Voxel[tile.XSize * tile.YSize * tile.ZSize];

        // Copy the floor tile from our tilesheet to this tile (playfield tiles only have one animation frame - 0)
        Helper.CopyVoxelsInBox(ref Tileset.Frames[(int)Tiles.Ground].Voxels, ref tile.Frames[0].Voxels, new PicaVoxelPoint(Tileset.Frames[0].XSize, Tileset.Frames[0].YSize, Tileset.Frames[0].ZSize), new PicaVoxelPoint(tile.XSize, tile.YSize, tile.ZSize), true);

        // Make some random grass
        // Z value of 14 is 1 less than the ground layer at Z15
        int grassAmount = Random.Range(0, 30);
        for (int i = 0; i < grassAmount; i++)
            tile.Frames[0].Voxels[Random.Range(0, 16) + tile.XSize * (Random.Range(0, 16) + tile.YSize * 14)] = new Voxel()
            {
                State = VoxelState.Active,
                Color = new Color(Random.Range(0f, 0.2f), Random.Range(0.5f, 0.8f), Random.Range(0f, 0.2f)), // This is a random green color
                Value = 1 // We'll give the grass a value of 1 so we can distinguish it later
            };

        // Maybe a random scenery tile if not in/near the walls?
        int tree = Random.Range(0, 25);
        if (tree < 4 && tileY>1 && tileY<Y_TILES-2)
        {
            Helper.CopyVoxelsInBox(ref Tileset.Frames[tree + 1].Voxels, ref tile.Frames[0].Voxels, new PicaVoxelPoint(Tileset.Frames[0].XSize, Tileset.Frames[0].YSize, Tileset.Frames[0].ZSize), new PicaVoxelPoint(tile.XSize, tile.YSize, tile.ZSize), true);
        }

        tile.UpdateAllChunksNextFrame();
    }
}
