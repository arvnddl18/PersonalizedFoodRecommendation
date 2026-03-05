using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Capstone.Data
{
    /// <summary>
    /// Exports the full database to a single JSON file (flat table-by-table structure)
    /// for submission to panelists (e.g. alongside .bacpac/.bak when JSON is required).
    /// </summary>
    public static class DatabaseExport
    {
        /// <summary>
        /// Export all tables from the database to one JSON file.
        /// Each table is exported as a key with an array of row objects (scalar columns only, no navigation properties).
        /// </summary>
        /// <param name="context">DbContext</param>
        /// <param name="outputPath">Full path for the output JSON file (e.g. DatabaseExport.json)</param>
        public static void ExportToJson(AppDbContext context, string outputPath)
        {
            var export = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Discover all DbSet<> properties on AppDbContext
            var dbSetProps = typeof(AppDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ToList();

            foreach (var prop in dbSetProps)
            {
                var entityType = prop.PropertyType.GetGenericArguments()[0];
                var tableName = prop.Name; // e.g. "Users", "FoodTypes"
                var list = QueryAllAsDictionaries(context, entityType);
                export[tableName] = list;
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            File.WriteAllText(outputPath, JsonSerializer.Serialize(export, options));
        }

        private static List<Dictionary<string, object?>> QueryAllAsDictionaries(AppDbContext context, Type entityType)
        {
            var setMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes);
            if (setMethod == null) return new List<Dictionary<string, object?>>();
            var genericSet = setMethod.MakeGenericMethod(entityType);
            var set = genericSet.Invoke(context, null);
            if (set == null) return new List<Dictionary<string, object?>>();

            // IQueryable: AsNoTracking().ToList()
            var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "AsNoTracking" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1);
            var asNoTracking = asNoTrackingMethod.MakeGenericMethod(entityType);
            var query = asNoTracking.Invoke(null, new[] { set });
            if (query == null) return new List<Dictionary<string, object?>>();

            var toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!;
            var toListGeneric = toListMethod.MakeGenericMethod(entityType);
            var list = toListGeneric.Invoke(null, new[] { query }) as System.Collections.IEnumerable;
            if (list == null) return new List<Dictionary<string, object?>>();

            var scalarProps = GetScalarProperties(entityType);
            var result = new List<Dictionary<string, object?>>();
            foreach (var entity in list)
            {
                if (entity == null) continue;
                var row = new Dictionary<string, object?>();
                foreach (var prop in scalarProps)
                {
                    try
                    {
                        var value = prop.GetValue(entity);
                        row[prop.Name] = value;
                    }
                    catch { /* skip if getter fails */ }
                }
                result.Add(row);
            }
            return result;
        }

        private static List<PropertyInfo> GetScalarProperties(Type entityType)
        {
            return entityType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                {
                    if (p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() != null)
                        return false;
                    var t = p.PropertyType;
                    if (t.IsPrimitive || t == typeof(string) || t == typeof(DateTime) || t == typeof(DateTime?) ||
                        t == typeof(decimal) || t == typeof(decimal?) || t == typeof(Guid) || t == typeof(Guid?) ||
                        t == typeof(TimeSpan) || t == typeof(TimeSpan?) || t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?))
                        return true;
                    if (Nullable.GetUnderlyingType(t) != null)
                    {
                        var u = Nullable.GetUnderlyingType(t)!;
                        return u.IsPrimitive || u == typeof(DateTime) || u == typeof(decimal) || u == typeof(Guid);
                    }
                    return false;
                })
                .ToList();
        }
    }
}
