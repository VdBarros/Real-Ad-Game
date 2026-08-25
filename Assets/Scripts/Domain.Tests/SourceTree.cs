using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Game.Domain.Tests
{
    static class SourceTree
    {
        public static string Root([CallerFilePath] string sourceFile = "")
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFile));
            while (directory != null && directory.Name != "Scripts")
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "No Scripts folder above " + sourceFile + ".");
            return directory.FullName;
        }

        public static string Read(params string[] pathParts)
        {
            var path = Path.Combine(Root(), Path.Combine(pathParts));

            Assert.That(File.Exists(path), Is.True, "No source at " + path + ", so this guard went blind.");

            return File.ReadAllText(path);
        }

        public static string PathTo(string fileName)
        {
            var found = Directory.GetFiles(Root(), fileName, SearchOption.AllDirectories).SingleOrDefault();

            Assert.That(found, Is.Not.Null, "No single " + fileName + " under the source tree.");
            return found;
        }
    }
}
