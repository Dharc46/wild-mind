using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class RoomManager : MonoBehaviour
{
    [Serializable]
    public class Count
    {
        public int minimum;
        public int maximum;

        public Count(int min, int max)
        {
            minimum = min;
            maximum = max;
        }
    }

    // Arrays of tile prefabs
    public GameObject[] floorTiles;
    public GameObject[] topWallsTiles;
    public GameObject[] bottomWallsTiles;
    public GameObject[] leftWallsTiles;
    public GameObject[] rightWallsTiles;
    public GameObject topLeftCornerTile;
    public GameObject topRightCornerTile;
    public GameObject bottomLeftCornerTile;
    public GameObject bottomRightCornerTile;

    public List<Doorway> DoorwayPrefabs;
    public GameObject switchPrefab;
    public GameObject obstaclePrefab;
    [SerializeField, Min(0)] private int obstacleCount = 3;
    [Header("Training Settings")]
    public bool isTrainingScene = false;
    public GameObject[] enemyPrefabs;

    // A list of possible locations to place tiles.
    //private List<Vector3> gridPositions = new List<Vector3>();

    private int _adjacentOffsetX;
    private int _adjacentOffsetY;

    private Room room;
    private GameObject switchInstance;

    private List<Position> _adjacentRooms;

    public Room GenerateRoom(int offsetX, int offsetY, List<Position> doorways, int roomId)
    {
        _adjacentOffsetX = offsetX;
        _adjacentOffsetY = offsetY;

        _adjacentRooms = doorways;

        room = new Room(roomId);

        GenerateWallsAndFloors();
        GenerateDoorways();
        GenerateObjects();
        GenerateObstacles();
        GenerateEntities();

        return room;
    }

    private void GenerateObjects()
    {
        Vector2 randPos = isTrainingScene
            ? GetRandomPositionInCenteredRoom()
            : Const.GetRandomPosition(_adjacentOffsetX, _adjacentOffsetY);
        switchInstance = Instantiate(switchPrefab, randPos, Quaternion.identity);
        switchInstance.transform.SetParent(room.Holder);
        room.Switch = switchInstance;
    }

    private void GenerateObstacles()
    {
        if (isTrainingScene)
        {
            return;
        }

        if (obstaclePrefab == null || obstacleCount <= 0)
        {
            return;
        }

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector2 randPos = isTrainingScene
                ? GetRandomPositionInCenteredRoom()
                : Const.GetRandomPosition(_adjacentOffsetX, _adjacentOffsetY);
            var obstacle = Instantiate(obstaclePrefab, randPos, Quaternion.identity, room.Holder);
            Debug.Log($"Spawn obstacle tại: {randPos} | Offset: ({_adjacentOffsetX}, {_adjacentOffsetY})");
        }
    }

    // TODO: Move to another class with all the Screen and camera stuff.
    public static Vector2 GetRandomPositionInCenteredRoom()
    {
        return Const.GetRandomPosition(0, 0);
    }

    // Generate the walls and floor of the room, randomazing the various varieties
    void GenerateWallsAndFloors()
    {
        for (int y = 0; y < Const.MapHeight; y++)
        {
            for (int x = 0; x < Const.MapWitdth; x++)
            {
                GameObject tile;

                // Corner tiles
                if (x == 0 && y == 0)
                {
                    tile = bottomLeftCornerTile;
                }
                else if (x == 0 && y == Const.MapHeight - 1)
                {
                    tile = topLeftCornerTile;
                }
                else if (x == Const.MapWitdth - 1 && y == 0)
                {
                    tile = bottomRightCornerTile;
                }
                else if (x == Const.MapWitdth - 1 && y == Const.MapHeight - 1)
                {
                    tile = topRightCornerTile;
                }
                //random left - hand walls, right walls, top, bottom
                else if (x == 0)
                {

                    tile = leftWallsTiles[Random.Range(0, leftWallsTiles.Length)];
                }
                else if (x == Const.MapWitdth - 1)
                {

                    tile = rightWallsTiles[Random.Range(0, rightWallsTiles.Length)];
                }
                else if (y == 0)
                {
                    tile = bottomWallsTiles[Random.Range(0, topWallsTiles.Length)];
                }
                else if (y == Const.MapHeight - 1)
                {
                    tile = topWallsTiles[Random.Range(0, bottomWallsTiles.Length)];
                }
                // if it's not a corner or a wall tile, be it a floor tile
                else
                {
                    tile = floorTiles[Random.Range(0, floorTiles.Length)];
                }

                Vector3 position = new Vector3(x + Const.MapRenderOffsetX + _adjacentOffsetX, 
                    y + Const.MapRenderOffsetY + _adjacentOffsetY, 0f);

                GameObject instance = Instantiate(tile, position, Quaternion.identity);

                instance.transform.SetParent(room.Holder);
            }
        }
    }

    void GenerateDoorways()
    {
        foreach (var roomPosition in _adjacentRooms)
        {
            Doorway doorway = Instantiate(DoorwayPrefabs.Find(x => x.position == roomPosition));
            doorway.Reposition(_adjacentOffsetX, _adjacentOffsetY);
            doorway.transform.SetParent(room.DoorwayHolder);

            room.Doorways.Add(doorway);
        }
    }

    void GenerateEntities()
    {
        foreach (var enemy in enemyPrefabs)
        {
            Vector2 randPos = isTrainingScene
                ? GetRandomPositionInCenteredRoom()
                : Const.GetRandomPosition(_adjacentOffsetX, _adjacentOffsetY);
            GameObject enemiInstance = Instantiate(enemy, randPos, Quaternion.identity);
            enemiInstance.transform.SetParent(room.Holder);
        }
    }
}
