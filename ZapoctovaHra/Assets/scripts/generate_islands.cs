﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class generate_islands : MonoBehaviour
{

	// Start is called before the first frame update
	private menu_handler menu_handler;
	void Start()
    {
		menu_handler = FindObjectOfType<menu_handler>();
		WorldLoader.LoadMap(menu_handler.world_name + ".world",32);
		Destroy(menu_handler.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}

public class WorldLoader
{
	public static byte[][] world_map;
	public static int world_size;
	private static System.Random rand;
	
	private static void WriteMap(string filePath)
	{
		byte[] serializedMap = new byte[world_size * world_size + 16]; //16 je velikost hlavicky souboru s velikosti mapy a pozici hrace
		
		//tvorba hlavicky
		byte[] worldSizeinByteArray = BitConverter.GetBytes(world_size);
		for(int i = 0; i < 4; i++)
		{
			serializedMap[i] = worldSizeinByteArray[i];
		}
		
		for(int i = 0; i < world_size; i++)
		{
			for(int j = 0; j < world_size; j++)
			{
				serializedMap[16 + i * world_size + j] = world_map[i][j];
			}
		}
		File.WriteAllBytes(filePath, serializedMap);
	}
		
	private static void ReadMap(string filePath)
	{
		byte[] serializedMap;
		
		//prvni precteme hlavicku souboru
		byte[] header = new byte[16];
		using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
		{
			reader.Read(header, 0, 16);
		}
		world_size = BitConverter.ToInt32(header,0);
		byte size_as_power_of_2 = (byte)System.Math.Log(world_size, 2);
		Debug.Log(size_as_power_of_2);
		
		//az potom data o svete
		serializedMap = File.ReadAllBytes(filePath);
		for(int i = 0; i < serializedMap.Length - 16; i++)
		{
			world_map[i >> size_as_power_of_2][i % world_size] = serializedMap[i + 15];
		}

		Debug.Log("Cteni dokonceno!");
		
		
	}
	
	private static void PerlinGenerate()
	{
		rand = new System.Random(); //mohl by chtit seed
		int perlin_offset = 0;  //rand.Next(0, int.MaxValue);
		for(int y = 0; y < world_size; y++)
		{
			for(int x = 0; x < world_size; x++)
			{
				world_map[y][x] = (byte)Mathf.Round(Mathf.PerlinNoise((10 * (float)x / 128) + perlin_offset, (10 * (float)y / 128) + perlin_offset) / 1.3f);
				world_map[y][x] *= RandomPOIDistribution();
			}
		}
	}

	private static byte RandomPOIDistribution()
	{
		int number = rand.Next(1, 17);
		int value_island = -(int)Math.Floor(Math.Sqrt(-number + 16)) + 4;

		return (byte)value_island;
	}

	//VELIKOST MAPY MUSI BYT MOCNINA 2!!!
	public static void LoadMap(string filePath, int size){
		world_size = size;
		world_map = new byte[world_size][];			
		for(int i = 0; i < world_size; i++)
		{
			world_map[i] = new byte[world_size];
		}
		
		if (File.Exists("Saves/" + filePath)) 
		{
			Debug.Log("Soubor existuje.");
			ReadMap("Saves/" + filePath);
		}
		
		else
		{
			//pokud soubor s mapou neexistoval, generuje se novy svet dane velikosti
			PerlinGenerate();
			WriteMap("Saves/" + filePath);
			Debug.Log("Soubor vytvoren.");			
		}
		
	}
}

public class Chunk : ScriptableObject
{
	const byte size = 16;
	int[] value_array = new int[4] { 4, 8, 2, 1 };
	List<GameObject> existingIslands = new List<GameObject>();
	public int pos_x;
	public int pos_y;
	
	private GameObject new_island;
	private GameObject new_POI;
	
	int type_of_island;
	Tuple<GameObject, int> parameters;
	
	//islands
	static GameObject isolated = Resources.Load("prefabs/isolated") as GameObject;
	static GameObject hill = Resources.Load("prefabs/hill") as GameObject;
	static GameObject L = Resources.Load("prefabs/L") as GameObject;
	static GameObject one_line = Resources.Load("prefabs/shore") as GameObject;
	static GameObject two_lines = Resources.Load("prefabs/bridge") as GameObject;
	static GameObject full = Resources.Load("prefabs/inside") as GameObject;
	
	//POIs
	static GameObject orange_tree = Resources.Load("prefabs/orange_tree") as GameObject;
	static GameObject palm_tree = Resources.Load("prefabs/palm_tree") as GameObject;
	static GameObject fortress = Resources.Load("prefabs/fortress") as GameObject;
	
	
	static Tuple<GameObject, int>[] models = new Tuple<GameObject, int>[16]{
		new Tuple<GameObject, int>(isolated, 0),
		new Tuple<GameObject, int>(hill, 270),
		new Tuple<GameObject, int>(hill, 0),
		new Tuple<GameObject, int>(L, 90),
		new Tuple<GameObject, int>(hill, 90),
		new Tuple<GameObject, int>(two_lines, 0),
		new Tuple<GameObject, int>(L, 180),
		new Tuple<GameObject, int>(one_line, 270),
		new Tuple<GameObject, int>(hill, 180),
		new Tuple<GameObject, int>(L, 0),
		new Tuple<GameObject, int>(two_lines, 90),
		new Tuple<GameObject, int>(one_line, 180),
		new Tuple<GameObject, int>(L, 270),
		new Tuple<GameObject, int>(one_line, 90),
		new Tuple<GameObject, int>(one_line, 0),
		new Tuple<GameObject, int>(full, 0)
	};
	
	
	static GameObject[] POIs_array = new GameObject[]{
		orange_tree,
		palm_tree,
		fortress
	};
	
	
	//inits matrix of given size
	public Chunk(int pos_x, int pos_y)
	{
		this.pos_x = pos_x;
		this.pos_y = pos_y;
	}
	
	public void InstinScene(){	
	
	for(int y = 0; y < size; y++)
		{
			for(int x = 0; x < size; x++)
			{
				if(ReturnPositionWorldMap(y, x) > 0)
				{
					type_of_island = TypeOfIsland(y, x);
					parameters = models[type_of_island];
					new_island = Instantiate(parameters.Item1,new Vector3(pos_x + x * 10, 0, pos_y + y * 10), Quaternion.identity);
					existingIslands.Add(new_island);
					new_island.GetComponent<Transform>().rotation = Quaternion.Euler(0f, (float)parameters.Item2, 0f);
				}
				if(ReturnPositionWorldMap(y, x) > 1)
				{
					int POIid = ReturnPositionWorldMap(y, x) - 2;
					new_POI = Instantiate(POIs_array[POIid],new Vector3(pos_x + x * 10, 10, pos_y + y * 10), Quaternion.identity);
					existingIslands.Add(new_POI);
				}
			}
		}
	}
	
	private byte ReturnPositionWorldMap(int y, int x)
	{
		byte value = WorldLoader.world_map[((y + (pos_y / 10)) % WorldLoader.world_size + WorldLoader.world_size) % WorldLoader.world_size][((x + (pos_x / 10)) % WorldLoader.world_size + WorldLoader.world_size) % WorldLoader.world_size];
		return value;
	}
	public void RemoveIslands()
	{
		foreach (GameObject island in existingIslands){
			Destroy(island);
		}
	
	}
	
	//prints matrix to Debug.Log
	public void printChunk()
	{
		string matice = System.Environment.NewLine;
		for(int i = 0; i < size; i++)
		{
			for(int j = 0; j < size; j++){
				matice = matice + ReturnPositionWorldMap(i, j) + " ";
			}
			matice += System.Environment.NewLine;
		}
		Debug.Log(matice);
	}

	private int TypeOfIsland(int x, int y)
	{
		int value_of_island = 0;
		int pointer = 0;
		for (int dx = -1; dx < 2; dx++)
		{
			for (int dy = -1; dy < 2; dy++)
			{
				if (Mathf.Abs(dx) + Mathf.Abs(dy) == 1)
				{
					if ((x + dx <= size) && (y + dy <= size) && (x + dx >= -1) && (y + dy >= -1))
					{
						if (ReturnPositionWorldMap(x + dx, y + dy) > 0)
						{
							value_of_island += value_array[pointer];
						}
					}
				pointer += 1;
				}
			}
		}
		return value_of_island;
	}
	
}
