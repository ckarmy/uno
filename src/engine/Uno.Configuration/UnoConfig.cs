﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Uno.Configuration.Format;
using Uno.Diagnostics;
using Uno.IO;

namespace Uno.Configuration
{
    public class UnoConfig
    {
        static UnoConfig _current;
        public static UnoConfig Current => _current ?? (_current = new UnoConfig());

        public static UnoConfig Get(IPathObject obj)
        {
            return Current.GetDirectoryConfig(Path.GetDirectoryName(obj.FullPath));
        }

        readonly List<UnoConfigFile> _files = new List<UnoConfigFile>();
        readonly Dictionary<string, UnoConfig> _configCache = new Dictionary<string, UnoConfig>();
        readonly Dictionary<string, UnoConfigFile> _fileCache = new Dictionary<string, UnoConfigFile>();
        readonly Dictionary<string, List<UnoConfigString>> _stringCache = new Dictionary<string, List<UnoConfigString>>();
        readonly HashSet<string> _visitedDirectories = new HashSet<string>();

        public IReadOnlyList<UnoConfigFile> Files => _files;

        public UnoConfig GetDirectoryConfig(string dir)
        {
            UnoConfig config;
            lock (_configCache)
                if (!_configCache.TryGetValue(dir, out config))
                    config = _configCache[dir] = new UnoConfig(this, dir);

            return config;
        }

        UnoConfigFile GetFile(string filename)
        {
            UnoConfigFile file;
            lock (_fileCache)
                if (!_fileCache.TryGetValue(filename, out file))
                    file = _fileCache[filename] = new UnoConfigFile(filename);

            return file;
        }

        public void Clear()
        {
            lock (_configCache)
                _configCache.Clear();
            lock (_fileCache)
                _fileCache.Clear();
        }

        public UnoConfig Copy()
        {
            return new UnoConfig(this);
        }

        public Dictionary<string, StuffItem> Flatten()
        {
            var result = new Dictionary<string, StuffItem>();

            foreach (var f in Files)
                foreach (var e in f.GetData())
                    result[e.Key] = e.Value;

            return result;
        }

        public string[] GetFullPathArray(string key)
        {
            var strings = GetStrings(key);
            var result = new string[strings.Count];
            for (int i = 0; i < strings.Count; i++)
                result[i] = strings[i].Value.ToFullPath(strings[i].ParentDirectory);
            return result;
        }

        public string[] GetFullPathArray(string key1, string key2)
        {
            var strings1 = GetStrings(key1);
            var strings2 = GetStrings(key2);
            var result = new string[strings1.Count + strings2.Count];
            for (int i = 0; i < strings1.Count; i++)
                result[i] = strings1[i].Value.ToFullPath(strings1[i].ParentDirectory);
            for (int i = 0; i < strings2.Count; i++)
                result[i + strings1.Count] = strings2[i].Value.ToFullPath(strings2[i].ParentDirectory);
            return result;
        }

        public string GetFullPath(string key, bool throwIfNull = true)
        {
            var strings = GetFullPathArray(key);

            if (strings.Length == 1)
                return strings[0];

            if (throwIfNull)
                throw new ArgumentException("Path " + key.Quote() + " was not found in .unoconfig");

            return null;
        }

        public string GetFullPath(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
                throw new ArgumentNullException(nameof(keys));

            foreach (var key in keys)
            {
                var result = GetFullPath(key, false);
                if (result != null)
                    return result;
            }

            return GetFullPath(keys[0]);
        }

        public int GetInt(string key, int def)
        {
            return int.Parse(GetString(key) ?? def.ToString());
        }

        public bool GetBool(string key, bool def = false)
        {
            return bool.Parse(GetString(key) ?? def.ToString());
        }

        public string[] GetStringArray(string key)
        {
            return GetStrings(key).Select(x => x.Value).ToArray();
        }

        public string GetString(string key)
        {
            var strings = GetStrings(key);
            switch (strings.Count)
            {
                case 0:
                    return null;
                case 1:
                    return strings[0].Value;
                default:
                    return string.Join("\n", GetStrings(key).Select(x => x.Value));
            }
        }

        public IReadOnlyList<UnoConfigString> GetStrings(string key)
        {
            List<UnoConfigString> result;
            lock (_stringCache)
            {
                if (_stringCache.TryGetValue(key, out result))
                    return result;

                var foundValues = new HashSet<string>();

                result = new List<UnoConfigString>();
                foreach (var file in _files)
                {
                    StuffItem item;
                    if (!file.GetData().TryGetValue(key, out item))
                        continue;

                    for (; item != null; item = item.Next)
                    {
                        var cacheKey = (item.File.Filename + ":" + item.Key + ":" + item.LineNumber).ToUpperInvariant();
                        if (foundValues.Contains(cacheKey))
                            continue;

                        foundValues.Add(cacheKey);

                        if (item.Type != StuffItemType.Append)
                            result.Clear();

                        if (string.IsNullOrEmpty(item.Value))
                            continue;

                        var index = 0;
                        foreach (var line in item.Value.Split('\n'))
                            if (!string.IsNullOrEmpty(line))
                                result.Insert(
                                    index++, 
                                    new UnoConfigString
                                    {
                                        ParentDirectory = item.File.ParentDirectory,
                                        Value = line
                                    });
                    }
                }

                _stringCache[key] = result;
            }

            return result;
        }

        UnoConfig()
        {
            LoadRecursive(GetAssemblyDirectory(Assembly.GetEntryAssembly()));
            LoadRecursive(GetAssemblyDirectory(typeof(UnoConfig).Assembly));

            try
            {
                LoadRecursive(Directory.GetCurrentDirectory());
            }
            catch (FileNotFoundException)
            {
                // Ignore this
                // GetCurrentDirectory() might throw exception on macOS if working directory is deleted
            }
        }

        UnoConfig(UnoConfig parent)
        {
            _files.AddRange(parent._files);
        }

        UnoConfig(UnoConfig parent, string dir)
            : this(parent)
        {
            LoadRecursive(dir);
        }

        void LoadRecursive(string dir)
        {
            if (string.IsNullOrEmpty(dir))
                return;

            // Convert drive letter to uppercase to avoid mixed case bugs
            if (PlatformDetection.IsWindows && dir.Length >= 2 && dir[1] == ':' && char.IsLower(dir[0]))
                dir = char.ToUpper(dir[0]) + dir.Substring(1);

            if (_visitedDirectories.Contains(dir))
                return;

            _visitedDirectories.Add(dir);

            var node_modules = Path.Combine(dir, "node_modules");

            if (Directory.Exists(node_modules))
                LoadNodeModules(node_modules);

            var filename = Path.Combine(dir, ".unoconfig");

            if (!File.Exists(filename))
                LoadRecursive(Path.GetDirectoryName(dir));
            else
                LoadFile(dir, filename);
        }

        void LoadNodeModules(string node_modules)
        {
            foreach (var dir in Directory.EnumerateDirectories(node_modules))
            {
                if (Path.GetFileName(dir).StartsWith("@"))
                {
                    LoadNodeModules(dir);
                    continue;
                }

                var filename = Path.Combine(dir, ".unoconfig");

                if (File.Exists(filename))
                    LoadFile(dir, filename, false);
            }
        }

        void LoadFile(string dir, string filename, bool scanParentDir = true)
        {
            var file = GetFile(filename);

            // Avoid overriding core settings when running an uno.exe other than 'src/main/Uno.CLI.Main/bin/$(Configuration)/uno.exe',
            // to make sure assembly paths remain consistent.
            StuffItem skipIfNotRoot;
            if (_files.Count > 0 &&
                file.GetData().TryGetValue("SkipIfNotRoot", out skipIfNotRoot) && bool.Parse(skipIfNotRoot.Value))
                return;

            // Don't load files that are already loaded
            foreach (var f in _files)
                if (f.GetData().ContainsFile(file.Stuff.Filename))
                    return;

            _files.Add(file);
            
            // Load parent configuration files
            StuffItem isRoot;
            if (scanParentDir && file.GetData().TryGetValue("IsRoot", out isRoot) && !bool.Parse(isRoot.Value))
                LoadRecursive(Path.GetDirectoryName(dir));
        }

        public static string GetAssemblyDirectory(Assembly asm)
        {
            if (asm == null)
                return null;
            var uri = new UriBuilder(asm.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public override string ToString()
        {
            const int width = 26;

            var sb = new StringBuilder();
            var keySet = new HashSet<string>();

            foreach (var file in Files)
                foreach (var key in file.GetData().Keys)
                    keySet.Add(key);

            // Remove magic variables
            keySet.Remove("IsRoot");
            keySet.Remove("SkipIfNotRoot");

            var keys = keySet.ToArray();
            Array.Sort(keys);

            foreach (var key in keys)
                sb.AppendLine(key.PadRight(width) + " " +
                    ToString(key).Replace("\n", "\n " + new string(' ', width)));

            return sb.ToString().Trim();
        }

        string ToString(string key)
        {
            var strings = GetStrings(key);
            var lines = new List<string>();

            foreach (var str in strings)
                lines.Add(str.ToString());

            return string.Join("\n", lines);
        }
    }
}
