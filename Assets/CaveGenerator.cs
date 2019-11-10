using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class CaveGenerator : MonoBehaviour
{
    public int width;
    public int height;
    public int smoothnes;
    public bool cave;

    public string seed;
    public bool useRandomSeed;

    System.Random generator;

    [Range(1, 100)]
    public int randomFillPercent;

    int[,] map;

    void Start()
    {
        if (useRandomSeed)
        {
            seed = Time.realtimeSinceStartup.ToString();
        }
        generator = new System.Random(seed.GetHashCode());
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        map = new int[width, height];
        FillMap();
        for (int i = 0; i < smoothnes; ++i)
        {
            SmoothMap();
        }

        ProcessMap();

        int borderSize = 5;
        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); ++x)
        {
            for (int y = 0; y < borderedMap.GetLength(1); ++y)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                {
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }
        CaveMeshGenerator meshGen = GetComponent<CaveMeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }

    //void OnDrawGizmos() {
    //    if (map != null) {
    //        for (int x = 0; x < width; x ++) {
    //            for (int y = 0; y < height; y ++) {
    //                Gizmos.color = (map[x,y] == 1)?Color.black:Color.white;
    //                Vector3 pos = new Vector3(-width/2 + x + .5f,0, -height/2 + y+.5f);
    //                Gizmos.DrawCube(pos,Vector3.one);
    //            }
    //        }
    //    }
    //}

    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        int wallThresholdSize = 30;

        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(0);

        int roomThresholdSize = 30;

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

        survivingRooms.Sort();
        survivingRooms[0].m_IsMainRoom = survivingRooms[0].m_IsAccessibleFromMainRoom = true;
        ConnectClosestRooms(survivingRooms);
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibleFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibleFromMainRoom)
        {
            foreach (Room room in allRooms)
            {
                if (room.m_IsAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibleFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.m_ConnectedRooms.Count > 0)
                {
                    continue;
                }
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                {
                    continue;
                }

                for (int tileIndexA = 0; tileIndexA < roomA.m_EdgeTiles.Count; ++tileIndexA)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.m_EdgeTiles.Count; ++tileIndexB)
                    {
                        Coord tileA = roomA.m_EdgeTiles[tileIndexA];
                        Coord tileB = roomB.m_EdgeTiles[tileIndexB];
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

            if (possibleConnectionFound && !forceAccessibleFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if (forceAccessibleFromMainRoom && possibleConnectionFound)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceAccessibleFromMainRoom)
        {
            ConnectClosestRooms(allRooms, true);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);

        if (cave)
        {
            List<Coord> line = GetLine(tileA, tileB);
            foreach (Coord c in line)
            {
                    DrawCircle(c, 4);
            }
        }
        else
        {
            if (tileA.tileX < tileB.tileX)
            {
                for (int x = tileA.tileX; x < tileB.tileX; ++x)
                {
                    DrawSquare(new Coord(x, tileA.tileY), 1);
                }
            }
            else
            {
                for (int x = tileB.tileX; x < tileA.tileX; ++x)
                {
                    DrawSquare(new Coord(x, tileA.tileY), 1);
                }

            }
            if (tileA.tileY < tileB.tileY)
            {
                for (int y = tileA.tileY; y < tileB.tileY; ++y)
                {
                    DrawSquare(new Coord(tileB.tileX, y), 1);
                }
            }
            else
            {
                for (int y = tileB.tileY; y < tileA.tileY; ++y)
                {
                    DrawSquare(new Coord(tileB.tileX, y), 1);
                }
            }
        }
    }

    void DrawSquare(Coord c, int d)
    {
        for (int x = -d; x <= d; ++x)
        {
            for (int y = -d; y <= d; ++y)
            {
                int drawX = c.tileX + x;
                int drawY = c.tileY + y;
                if (CheckIfInMapBounds(drawX, drawY))
                {
                    map[drawX, drawY] = 0;
                }
            }
        }
    }

    void DrawCircle(Coord c, int r)
    {
        for(int x = -r; x <= r; ++x)
        {
            for (int y = -r; y <= r; ++y)
            {
                if (x * x + y * y <= r * r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if (CheckIfInMapBounds(drawX, drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - x;
        int dy = to.tileY - y;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Math.Abs(dx);
        int shortest = Math.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Math.Abs(dy);
            shortest = Math.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; ++i)
        {
            line.Add(new Coord(x, y));
            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
    }

    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                if(mapFlags[x, y] == 0 && map[x,y] == tileType)
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

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; ++x)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; ++y)
                {
                    if (CheckIfInMapBounds(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
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

    void SmoothMap()
    {
        int[,] newMap = new int [width, height];
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                newMap[x, y] = DetermineState(x, y);
            }
        }
        map = newMap;
    }

    int DetermineState(int x, int y)
    {
        if (cave)
        {
            int neighbourWalls = GetSurroundingWallCount(x, y);
            if (neighbourWalls > 4)
            {
                return 1;
            }
            else if (neighbourWalls < 4)
            {
                return 0;
            }
            return map[x, y];
        }
        else
        {
            if (!CheckIfInMapBounds(x - 1, y) || !CheckIfInMapBounds(x + 1, y) ||
                !CheckIfInMapBounds(x, y - 1) || !CheckIfInMapBounds(x, y + 1))
            {
                return 1;
            }

            if (!CheckIfInMapBounds(x - 2, y) || !CheckIfInMapBounds(x + 2, y) ||
                !CheckIfInMapBounds(x, y - 2) || !CheckIfInMapBounds(x, y + 2))
            {
                return map[x, y];
            }
            if ((map[x + 1, y] == 1 && map[x - 1 , y] == 1 && (map[x + 2, y] == 1 || map[x - 2, y] == 1)) ||
                (map[x, y + 1] == 1 && map[x, y - 1] == 1 && (map[x, y + 2] == 1 || map[x, y - 2] == 1)))
            {
                return 1;
            }
            return 0;
        }
    }

    bool CheckIfInMapBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; ++neighbourX)
        {
            for (int neighbourY = gridY- 1; neighbourY <= gridY + 1; ++neighbourY)
            {
                if (CheckIfInMapBounds(neighbourX, neighbourY))
                {
                    if ((neighbourX != gridX || neighbourY != gridY))
                    {
                        wallCount += map[neighbourX, neighbourY];
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

    void FillMap()
    {         
        for(int x = 0; x < width; ++x)
        {
            for(int y = 0; y < height; ++y)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (generator.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    class Room : IComparable<Room>
    {
        public List<Coord> m_Tiles;
        public List<Coord> m_EdgeTiles;
        public List<Room> m_ConnectedRooms;
        public int m_RoomSize;
        public bool m_IsAccessibleFromMainRoom;
        public bool m_IsMainRoom;
        
        public Room()
        {
        }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            m_Tiles = roomTiles;
            m_RoomSize = m_Tiles.Count;
            m_ConnectedRooms = new List<Room>();

            m_EdgeTiles = new List<Coord>();
            foreach (Coord tile in m_Tiles)
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; ++x)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; ++y)
                    {
                        if (x == tile.tileX || x == tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                m_EdgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        public void SetAccesibleFromMainRoom()
        {
            if(!m_IsAccessibleFromMainRoom)
            {
                m_IsAccessibleFromMainRoom = true;
                foreach (Room connectedRoom in m_ConnectedRooms)
                {
                    connectedRoom.SetAccesibleFromMainRoom();
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.m_IsAccessibleFromMainRoom)
            {
                roomB.SetAccesibleFromMainRoom();
            }
            else if (roomB.m_IsAccessibleFromMainRoom)
            {
                roomA.SetAccesibleFromMainRoom();
            }
            roomA.m_ConnectedRooms.Add(roomB);
            roomB.m_ConnectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return m_ConnectedRooms.Contains(otherRoom);
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.m_RoomSize.CompareTo(m_RoomSize);
        }
    }
}
