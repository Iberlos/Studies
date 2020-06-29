using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using System;

public class MapGenerator : MonoBehaviour
{
    [Header("UI")]
    public Toggle useRandomSeedToggle;
    public InputField desiredSeedInputField;
    public Slider fillPercentSlider;
    public Slider iterationsSlider;
    public Toggle useBacklogToggle;
    public Toggle useCoroutineToggle;
    public Slider timeBetweenStepsSlider;

    [Header("Map Dimentions")]
    public int width;
    public int height;
    public int borderSize;
    public float wallHeight;

    [Header("Random Fill Parameters")]
    public bool useRanomSeed;
    public string seed;

    [Range(0,100)]
    public int randomFilPercent;

    [Header("Smooth Settings")]
    [Range(0,10)]
    public int iterations;
    public int wallCountThreshold;
    public bool useMapBacklog;
    public bool useCorutine;
    public float timeBetweenSteps;

    int[,] map;
    int[,] mapBackLog;

    private void Start()
    {
        useRandomSeedToggle.isOn = useRanomSeed;
        desiredSeedInputField.text = seed;
        fillPercentSlider.value = randomFilPercent;
        iterationsSlider.value = iterations;
        useBacklogToggle.isOn = useMapBacklog;
        useCoroutineToggle.isOn = useCorutine;
        timeBetweenStepsSlider.value = timeBetweenSteps;

        GenerateMap();
    }

    public void GenerateMap()
    {
        map = new int[width, height];
        if(useMapBacklog)
        {
            mapBackLog = new int[width, height];
        }
        RanodmFillMap();
        if(useCorutine)
        {
            StartCoroutine(SmoothMapCorrutine(iterations));
        }
        else
        {
            SmoothMap(iterations);
        }

        SimplifyMap();

        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];
        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if(x>=borderSize && x<width+borderSize && y>=borderSize && y<height+borderSize )
                {
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }
        map = borderedMap;

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(map);

        //AlternateMeshGenerator meshGen = GetComponent<AlternateMeshGenerator>();
        //meshGen.GenerateMesh(map, 1);
    }

    List<List<Coord>> GetRegions(int a_tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for(int x= 0; x <width; x++)
        {
            for(int y =0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x,y] == a_tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach(Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    void SimplifyMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);
        int wallThreshold = 50;

        foreach(List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThreshold)
            {
                foreach(Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(0);
        int roomThresholdSize = 50;
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }

        ConnectClosestRooms(survivingRooms);
        ConnectAllRegions(survivingRooms);
    }

    void ConnectClosestRooms(List<Room> a_roomList)
    {
        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();

        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;


        foreach(Room roomA in a_roomList)
        {
            possibleConnectionFound = false;

            foreach (Room roomB in a_roomList)
            {
                if (roomA == roomB) continue;
                if (roomA.IsConnected(roomB))
                {
                    possibleConnectionFound = false;
                    break;
                }

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];

                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if(distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if(possibleConnectionFound)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }
    }

    void ConnectAllRegions(List<Room> a_roomList)
    {
        List<Room> allRooms = a_roomList;
        List<List<Room>> regions;

        do
        {
            regions = new List<List<Room>>();

            foreach (Room room in allRooms)//clean flags from previous iterations
            {
                room.flag = false;
            }

            foreach (Room room in allRooms)
            {
                if (room.flag == false)
                {
                    regions.Add(new List<Room>());
                    regions[regions.Count - 1].Add(room);
                    for (int i = 0; i < regions[regions.Count - 1].Count; i++)
                    {
                        regions[regions.Count - 1][i].flag = true;
                        foreach (Room conectedRoom in regions[regions.Count - 1][i].conectedRooms)
                        {
                            if (conectedRoom.flag == false)
                            {
                                conectedRoom.flag = true;
                                regions[regions.Count - 1].Add(conectedRoom);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < regions.Count; i++) //for each region
            {
                int bestDistance = 0;
                Coord bestTileA = new Coord();
                Coord bestTileB = new Coord();

                Room bestRoomA = new Room();
                Room bestRoomB = new Room();
                bool possibleConnectionFound = false;

                for (int j = 0; j < regions.Count; j++) //for each remaining region
                {
                    if (j == i) continue;
                    foreach (Room roomA in regions[i]) //for each room in the region being tested
                    {
                        foreach (Room roomB in regions[j]) //for each room in the other region being considered
                        {
                            for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                            {
                                for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                                {
                                    Coord tileA = roomA.edgeTiles[tileIndexA];
                                    Coord tileB = roomB.edgeTiles[tileIndexB];

                                    int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                                    if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                                    {
                                        bestDistance = distanceBetweenRooms;
                                        possibleConnectionFound = true;
                                        bestTileA = tileA;
                                        bestTileB = tileB;
                                        bestRoomA = roomA;
                                        bestRoomB = roomB;
                                    }
                                }
                            }
                        }
                    }
                }

                if (possibleConnectionFound)
                {
                    CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                }
            }
        } while (regions.Count > 1);
    }

    void CreatePassage(Room a_roomA, Room a_roomB, Coord a_tileA, Coord a_tileB)
    {
        Room.ConectRooms(a_roomA, a_roomB);
        Debug.DrawLine(CoordToWorld(a_tileA), CoordToWorld(a_tileB), Color.green, 5);

        List<Coord> line = GetLine(a_tileA, a_tileB);
        foreach(Coord c in line)
        {
            DrawCircle(c, 1);
        }
    }

    void DrawCircle(Coord c, int r)
    {
        for(int x = -r; x<r; x++)
        {
            for (int y = -r; y < r; y++)
            {
                if(x*x + y*y <= r*r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if(IsInMapRange(drawX,drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

    List<Coord> GetLine(Coord a_from, Coord a_to)
    {
        List<Coord> line = new List<Coord>();

        int x = a_from.tileX;
        int y = a_from.tileY;

        int dx = a_to.tileX - x;
        int dy = a_to.tileY - y;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if(longest < shortest)
        {
            inverted = true;

            //switch values
            int temp = longest;
            longest = shortest;
            shortest = temp;

            temp = step;
            step = gradientStep;
            gradientStep = temp;
        }

        int gradientAccumulation = longest / 2;

        for(int i = 0; i< longest+1; i++)
        {
            line.Add(new Coord(x, y));

            if(inverted) y += step;
            else x += step;

            gradientAccumulation += shortest;
            if(gradientAccumulation >= longest)
            {
                if (inverted) x += gradientStep;
                else y += gradientStep;
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    Vector3 CoordToWorld(Coord a_tile)
    {
        return new Vector3(-width/2 + 0.5f + a_tile.tileX, 2, -height/2 + 0.5f + a_tile.tileY);
    }

    List<Coord> GetRegionTiles(int a_startX, int a_startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[a_startX, a_startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(a_startX, a_startY));
        mapFlags[a_startX, a_startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX-1; x <= tile.tileX +1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if(IsInMapRange(x,y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if(mapFlags[x,y] == 0 && map[x,y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    bool IsInMapRange(int a_x, int a_y)
    {
        return a_x >= 0 && a_x < width && a_y >= 0 && a_y < height;
    }

    private void RanodmFillMap()
    {
        if(useRanomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for(int x = 0; x<width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if(x == 0 || x == width-1 || y == 0 || y == height-1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFilPercent) ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap(int iterations)
    {
        if (iterations == 0) return;

        iterations--;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = CountSurroundingWalls(x,y);

                if(neighbourWallTiles > wallCountThreshold)
                {
                    if(useMapBacklog)
                    {
                        mapBackLog[x, y] = 1;
                    }
                    else
                    {
                        map[x, y] = 1;
                    }
                }
                else if(neighbourWallTiles < wallCountThreshold)
                {
                    if (useMapBacklog)
                    {
                        mapBackLog[x, y] = 0;
                    }
                    else
                    {
                        map[x, y] = 0;
                    }
                }
            }
        }

        if (useMapBacklog)
        {
            ApplyBacklog();
        }
        SmoothMap(iterations);
    }

    IEnumerator SmoothMapCorrutine(int iterations)
    {
        iterations--;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = CountSurroundingWalls(x, y);

                if (neighbourWallTiles > wallCountThreshold)
                {
                    if (useMapBacklog)
                    {
                        mapBackLog[x, y] = 1;
                    }
                    else
                    {
                        map[x, y] = 1;
                    }
                }
                else if (neighbourWallTiles < wallCountThreshold)
                {
                    if (useMapBacklog)
                    {
                        mapBackLog[x, y] = 0;
                    }
                    else
                    {
                        map[x, y] = 0;
                    }
                }
            }
        }

        if (useMapBacklog)
        {
            ApplyBacklog();
        }

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(map);

        yield return new WaitForSeconds(timeBetweenSteps);
        if (iterations > 0) StartCoroutine(SmoothMapCorrutine(iterations));
    }

    private void ApplyBacklog()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x, y] = mapBackLog[x, y];
            }
        }
    }

    int CountSurroundingWalls( int gridX, int gridY)
    {
        int wallCount = 0;
        for(int neighborX = gridX-1; neighborX <= gridX+1; neighborX++)
        {
            for(int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if(neighborX>=0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        wallCount += map[neighborX, neighborY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int a_x, int a_y)
        {
            tileX = a_x;
            tileY = a_y;
        }
    }

    class Room
    {
        public bool flag = false; //Used in outside iterations
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> conectedRooms;
        public int roomSize;

        public Room()
        {
        }

        public Room(List<Coord> a_tiles, int[,] map)
        {
            tiles = a_tiles;
            roomSize = tiles.Count;
            conectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                for(int x = tile.tileX-1; x<= tile.tileX+1; x++)
                {
                    for(int y = tile.tileY -1; y< tile.tileY+1; y++)
                    {
                        if(x == tile.tileX || y == tile.tileY)
                        {
                            if(map[x,y] == 1)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }
        public static void ConectRooms(Room a_roomA, Room a_roomB)
        {
            if(!a_roomA.IsConnected(a_roomB)&& !a_roomB.IsConnected(a_roomA))
            {
                a_roomA.conectedRooms.Add(a_roomB);
                a_roomB.conectedRooms.Add(a_roomA);
            }
        }

        public bool IsConnected(Room a_otherRoom)
        {
            return conectedRooms.Contains(a_otherRoom);
        }
    }

    //Added getters for UI
    public void ToggleUseRandomSeed()
    {
        useRanomSeed = useRandomSeedToggle.isOn;
    }
    public void ToggleUseCoroutine()
    {
        useCorutine = useCoroutineToggle.isOn;
    }
    public void ToggleUseBacklog()
    {
        useMapBacklog = useBacklogToggle.isOn;
    }
    public void SetSeed()
    {
        seed = desiredSeedInputField.text;
    }
    public void SetFillPercent()
    {
        randomFilPercent = (int)(fillPercentSlider.value);
    }
    public void SetIterations()
    {
        iterations = (int)(iterationsSlider.value);
    }
    public void SetTimeBetweenSteps()
    {
        timeBetweenSteps = timeBetweenStepsSlider.value;
    }
    public void CloseApplication()
    {
        Application.Quit();
    }
}
