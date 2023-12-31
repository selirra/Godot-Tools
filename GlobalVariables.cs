// MIT License
// 
// Copyright (c) 2023 Kalmár Patrik
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// IMPORTANT: Do not forget to exclude the generated JSON file from the exported build otherwise
//            your build will use the exported one instead the one generated by the build!

using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// This class adds functionality similar to Unity's PlayerPrefs. <br/>
/// You can add it to your list of autoloaded scripts, and access it using `GlobalVariables.instance`
/// </summary>
public partial class GlobalVariables : Node
{
    public static GlobalVariables instance { get; private set; }
    private static readonly string filePath = "res://settings.json";
    private static readonly List<Type> supportedTypes = new List<Type>{
        typeof(string),
        typeof(int),
        typeof(float)
    };

    /* ------- Declare properties here --------- */

    public string PlayerName { get; set; }
    public float MovementSpeed { get; set; }

    /* ----------------------------------------- */

    /// <summary>
    /// Sets the fields in the GlobalVariables class to their default values,
    /// then serializes them into a json file at the path specified in the "filePath" field.
    /// </summary>
    private void Reset(){

        /* -------- Declare defaults here ---------- */

        PlayerName = "Player";
        MovementSpeed = 200f;

        /* ----------------------------------------- */

        Serialize();
    }

    /// <summary>
    /// Retrieves a collection of property information objects for properties declared in the GlobalVariables class with specific data types. <br/>
    /// Selects properties whose types are included in the supportedTypes list and are declared in the GlobalVariables class.
    /// </summary>
    /// <returns>An enumerable collection of these selected property information objects</returns>
    private IEnumerable<PropertyInfo> GetSupportedProperties(){
        return GetType().GetProperties().Where(
            property => supportedTypes.Contains(property.PropertyType)
            && property.DeclaringType == typeof(GlobalVariables)
        );
    }

    /// <summary>
    /// Serializes the fields contained in the GlobalVariables class into a
    /// json file at the path specified in the "filePath" field.
    /// </summary>
    private void Serialize(){
        try{
            var properties = GetSupportedProperties().ToDictionary(
                property => property.Name,
                property => property.GetValue(this)
            );
            
            var jsonOptions = new JsonSerializerOptions{WriteIndented = true};
            var json = JsonSerializer.Serialize(properties, jsonOptions);

            using (var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write)){
                file.StoreString(json);
            }
        }
        catch(Exception e){
            GD.PrintErr($"Error during serialization: {e.Message} \n {e.StackTrace}");
        }
    }

    /// <summary>
    /// Deserializes the json file at the path specified in the "filePath" field,
    /// then sets the fields of the GlobalVariables class accordingly.
    /// </summary>
    private void Deserialize(){
        try{
            if (!FileAccess.FileExists(filePath)){
                GD.PrintErr($"{filePath} does not exist, creating a new one.");
                Reset();
                return;
            }

            using (var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read)){
                var json = file.GetAsText();
                var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                foreach (var property in GetSupportedProperties()){
                    if (properties.ContainsKey(property.Name)){
                        JsonElement element = properties[property.Name];
                        object value = null;

                        if (property.PropertyType == typeof(string))
                            value = element.GetString();
                        else if (property.PropertyType == typeof(int))
                            value = element.GetInt32();
                        else if (property.PropertyType == typeof(float))
                            value = element.GetSingle();

                        property.SetValue(this, value);
                    }
                }
            }
        }
        catch (Exception e){
            GD.PrintErr($"Error during deserialization: {e.Message} \n {e.StackTrace}");
            Reset();
        }
    }

    public override void _EnterTree(){
        instance = this;
        Deserialize();
    }

    public override void _ExitTree(){
        Serialize();
    }
}
