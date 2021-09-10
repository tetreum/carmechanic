using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using SimpleJSON;
using SQLite4Unity3d;
using UnityEngine;

public class Service
{
    public static Dictionary<CarEngine.Part, PartStaticInfo> partsList = new();

    public static Dictionary<string, CarInfo> carList = new();
    public static SQLiteConnection _db;

    public static SQLiteConnection db
    {
        get
        {
            if (_db == null)
            {
                var DatabaseName = "CarMechanic.db";
                string dbPath;
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    dbPath = string.Format(@"Assets/StreamingAssets/{0}", DatabaseName);
                }
                else
                {
                    // check if file exists in Application.persistentDataPath
                    dbPath = string.Format("{0}/{1}", Application.persistentDataPath, DatabaseName);

                    if (!File.Exists(dbPath))
                    {
                        //string loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;
                        var loadDb = string.Format(@"Assets/StreamingAssets/{0}", DatabaseName);

                        // then save to Application.persistentDataPath
                        File.Copy(loadDb, dbPath);
                    }
                }

                _db = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            }

            return _db;
        }
        set { }
    }

    public static void resetDB()
    {
        //@ToDo replace CarMechanic.db with CarMechanicOriginal.db and execute this method before git commiting
    }

    public static T getOne<T>(Expression<Func<T, bool>> predExpr) where T : class, new()
    {
        try
        {
            return db.Table<T>().Where(predExpr).FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Parses all jsons to arrays
    public static void init()
    {
        var i = 0;
        var list = Utils.getJSON("Translation/parts");
        JSONNode item;

        while (i < list.Count)
        {
            item = list[i];

            partsList.Add((CarEngine.Part) item["id"].AsInt, new PartStaticInfo
            {
                Name = item["name"].Value,
                Description = item["description"].Value,
                Price = item["price"].AsInt
            });
            i++;
        }

        i = 0;
        list = Utils.getJSON("vehicles");

        while (i < list.Count)
        {
            item = list[i];

            carList.Add(item["folder"].Value, new CarInfo
            {
                name = item["name"].Value,
                folder = item["folder"].Value
            });
            i++;
        }
    }
}