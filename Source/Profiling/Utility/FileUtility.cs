using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace Analyzer.Profiling 
{
    public struct FileHeader : IEquatable<FileHeader>
    {
        // magic must equal 440985710, otherwise the file has been corrupted.
        public int MAGIC;
        public int scribingVer;
        public string methodName;
        public string name;
        public bool entryPerCall;
        public bool onlyEntriesWithValues;
        public int entries;
        public int targetEntries;
        
        public string Name => name == "" ? methodName : name;

        public static FileHeader Default => new FileHeader() {
            MAGIC = FileUtility.ENTRY_FILE_MAGIC, // used to verify the file has not been corrupted on disk somehow.
            scribingVer = FileUtility.SCRIBE_FILE_VER,
            entries = 0,
            targetEntries = Profiler.RECORDS_HELD,
            name = "" // default to an empty name
        };

        public static bool operator ==(FileHeader lhs, FileHeader rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(FileHeader lhs, FileHeader rhs) {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj) {
            return obj is FileHeader other && Equals(other);
        }

        public bool Equals(FileHeader other) {
            return MAGIC == other.MAGIC && scribingVer == other.scribingVer && methodName == other.methodName && name == other.name && entryPerCall == other.entryPerCall && onlyEntriesWithValues == other.onlyEntriesWithValues && entries == other.entries && targetEntries == other.targetEntries;
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = MAGIC;
                hashCode = (hashCode * 397) ^ scribingVer;
                hashCode = (hashCode * 397) ^ (methodName != null ? methodName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (name != null ? name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ entryPerCall.GetHashCode();
                hashCode = (hashCode * 397) ^ onlyEntriesWithValues.GetHashCode();
                hashCode = (hashCode * 397) ^ entries;
                hashCode = (hashCode * 397) ^ targetEntries;
                return hashCode;
            }
        }

    }

    public class EntryFile
    {
        public FileHeader header;
        public double[] times;
        public int[] calls;

    }

    public class FileWithHeader {

        public FileInfo info;
        public FileHeader header;

        public FileWithHeader(FileInfo info) {
            this.info = info;
            this.header = FileUtility.ReadHeader(info);
        }

    }
    
    public static class FileUtility
    {
        public const int ENTRY_FILE_MAGIC = 440985710;
        public const int SCRIBE_FILE_VER = 2;
        
        private static string INVALID_CHARS = $@"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]+";
        private static string GetFileLocation => Path.Combine(GenFilePaths.SaveDataFolderPath, "Analyzer");

        // file_name-NUMBER.data
        public static int GetFileNumber(FileInfo file) {
            var firstNumIdx = file.Name.LastIndexOf('-') + 1;
            var suffix = file.Name.Substring(firstNumIdx, file.Name.LastIndexOf('.') - firstNumIdx);
            return int.Parse(suffix);
        }

        private static string FinalFileNameFor(string str) {
            var prevEntries = PreviousEntriesFor(str).ToList();
            var number = prevEntries.Count == 0 ? 0 : GetFileNumber(prevEntries.MaxBy(fh => GetFileNumber(fh.info) + 1).info) + 1;
            return Path.Combine(GetFileLocation, SanitizeFileName(str) + '-' + number  + ".data");
        }
        
        private static List<FileWithHeader> cachedFiles = new List<FileWithHeader>();
        private static long lastFileAccess = 0;
        private static bool changed = true;

        private static void RefreshFiles()
        {
            var access = DateTime.Now.ToFileTimeUtc();

            if (!changed || access - lastFileAccess <= 15) return;
            
            var refreshedFiles = GetDirectory().GetFiles();
            cachedFiles = refreshedFiles.Select(file => new FileWithHeader(file)).ToList();
            
            lastFileAccess = access;
            changed = false;
        }
        
        public static DirectoryInfo GetDirectory()
        {
            var directory = new DirectoryInfo(GetFileLocation);
            if (!directory.Exists)
                directory.Create();

            return directory;
        }

        // taken in part from: https://stackoverflow.com/a/12924582, ignoring reserved kws.
        private static string SanitizeFileName(string filename) => Regex.Replace(filename, INVALID_CHARS, "_").Replace(' ', '_');

        public static IEnumerable<FileWithHeader> PreviousEntriesFor(string s)
        {
            RefreshFiles();
            
            var fn = SanitizeFileName(s);

            return cachedFiles?.Where(f => f.info.Name.Contains(fn)) ?? Enumerable.Empty<FileWithHeader>();
        }
        
        public static EntryFile ReadFile(FileInfo file)
        {
            var entryFile = new EntryFile();

            try
            {
                using (var reader = new BinaryReader(file.OpenRead()))
                {
                    entryFile.header = ReadHeader(reader);
                    if (entryFile.header.MAGIC == -1) return null;

                    // Backwards compatibility with the old version.
                    if (entryFile.header.scribingVer == 1) {
                        entryFile.header.scribingVer = 2;
                        if (entryFile.header.name == " ") {
                            entryFile.header.name = "";
                        }
                    } else if (entryFile.header.scribingVer != SCRIBE_FILE_VER) {
                        ThreadSafeLogger.Error($"Tried to load {file.Name}, this was created by a version of analyzer using an older file header. This data must be re-collected.");
                        return null;
                    }

                    entryFile.times = new double[entryFile.header.entries];
                    entryFile.calls = new int[entryFile.header.entries];
                    if (entryFile.header.entryPerCall)
                    {
                        Array.Fill(entryFile.calls, 1);
                    }

                    for (int i = 0; i < entryFile.header.entries; i++)
                    {
                        entryFile.times[i] = reader.ReadDouble();
                        if (!entryFile.header.entryPerCall)
                            entryFile.calls[i] = reader.ReadInt32();
                    }

                    reader.Close();
                    reader.Dispose();
                }
            }
            catch (Exception e)
            {
                ThreadSafeLogger.ReportException(e, "Failed while reading entry file from disk.");
            }
            
            return entryFile;
        }

        public static void WriteFile(EntryFile file)
        {
            var fileName = FinalFileNameFor(file.header.methodName);
            
            try
            {
                using (var writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    writer.Write(file.header.MAGIC);
                    writer.Write(file.header.scribingVer);
                    writer.Write(file.header.methodName);
                    writer.Write(file.header.name);
                    writer.Write(file.header.entryPerCall);
                    writer.Write(file.header.onlyEntriesWithValues);
                    writer.Write(file.header.entries);
                    writer.Write(file.header.targetEntries);

                    // interleaved is faster by profiling, (even if less cache-efficient) 
                    for (var i = 0; i < file.header.entries; i++)
                    {
                        writer.Write(file.times[i]);
                        if (!file.header.entryPerCall)
                            writer.Write(file.calls[i]);
                    }
                    
                    writer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception e)
            {
                ThreadSafeLogger.ReportException(e, $"Caught an exception when writing file to disk, if the file exists on disk, it should be deleted at {fileName}");
            }
 

            changed = true;
        }

        public static FileHeader ReadHeader(FileInfo file)
        {
            return ReadHeader(new BinaryReader(file.OpenRead()));
        }

        public static FileHeader ReadHeader(BinaryReader reader)
        {
            var fileHeader = new FileHeader()
            {
                MAGIC = reader.ReadInt32(),
                scribingVer = reader.ReadInt32(),
                methodName = reader.ReadString(),
                name = reader.ReadString(),
                entryPerCall = reader.ReadBoolean(),
                onlyEntriesWithValues = reader.ReadBoolean(),
                entries = reader.ReadInt32(),
                targetEntries = reader.ReadInt32()
            };

            if (fileHeader.MAGIC == ENTRY_FILE_MAGIC) return fileHeader;
            
            ThreadSafeLogger.Error($"Loaded header has an invalid MAGIC number, this indicates disk corruption");
            return new FileHeader() { MAGIC = -1 }; // magic = -1 is an error value. 
        }

        public static void DeleteFile(FileInfo file) {
            file.Delete();
            changed = true;
        }
        
        
        
    }
}
