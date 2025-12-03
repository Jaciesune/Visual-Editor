using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using VE.Models;

namespace VE.Services
{
    public static class ProjectStorageService
    {
        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public static void SaveProject(ProjectFile project, string path)
        {
            var json = JsonSerializer.Serialize(project, _jsonOptions);
            File.WriteAllText(path, json);
        }

        public static ProjectFile LoadProject(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ProjectFile>(json, _jsonOptions);
        }
    }
}