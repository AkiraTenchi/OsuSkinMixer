using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using OsuSkinMixer.Statics;
using File = System.IO.File;

namespace OsuSkinMixer.Models;

public class OsuSkin
{
    public OsuSkin(string name, DirectoryInfo dir, bool hidden = false)
    {
        Name = name;
        Directory = dir;
        SkinIni = new OsuSkinIni(name, "osu! skin mixer by rednir");
        Hidden = hidden;
    }

    public OsuSkin(DirectoryInfo dir, bool hidden = false)
    {
        Name = dir.Name;
        Directory = dir;
        Hidden = hidden;
        if (File.Exists($"{dir.FullName}/skin.ini"))
        {
            try
            {
                SkinIni = new OsuSkinIni(File.ReadAllText($"{dir.FullName}/skin.ini"));
            }
            catch (Exception ex)
            {
                GD.PushError($"Failed to parse skin.ini for skin '{Name}' due to exception {ex.Message}");
                OS.Alert($"Skin.ini parse error for skin '{Name}', please report this error!\n\n{ex.Message}");
            }
        }
    }

    public string Name { get; set; }

    public DirectoryInfo Directory { get; set; }

    public OsuSkinIni SkinIni { get; set; }

    public bool Hidden { get; set; }

    public override string ToString()
        => Name;

    public override bool Equals(object obj)
        => obj is OsuSkin skin && Name == skin?.Name;

    public override int GetHashCode()
        => Name.GetHashCode();

    public Texture2D GetTexture(string filename)
    {
        if (_textureCache.TryGetValue(filename, out Texture2D value))
            return value;

        Settings.Log($"Loading texture {filename} for skin: {Name}");

        string path = $"{Directory.FullName}/{filename}";
		Image image = new();

        if (!File.Exists(path))
        {
            Settings.Log("Falling back to default texture.");
            var defaultTexture = GetDefaultTexture(filename);
            _textureCache.Add(filename, defaultTexture);
            return GetDefaultTexture(filename);
        }

		Error err = image.Load(path);

        if (err != Error.Ok)
            return null;

        var texture = ImageTexture.CreateFromImage(image);
        _textureCache.Add(filename, texture);
        return texture;
    }

    public void ClearTextureCache()
        => _textureCache.Clear();

    private readonly Dictionary<string, Texture2D> _textureCache = new();

    private static Texture2D GetDefaultTexture(string filename)
        => GD.Load<Texture2D>($"res://assets/defaultskin/{filename}");
}