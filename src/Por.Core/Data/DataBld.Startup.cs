using System;
using System.IO;
using System.Security;

namespace Por.Core
{
    public static partial class DataBld
    {
        public static void BuildDataFiles()
        {
            DaxIo.ResetArchiveCache();

            bool[] stale = new bool[C_Data.FAMILIES + 1];
            bool anyStale = false;

            for (byte fam = 1; fam <= C_Data.FAMILIES; fam++)
            {
                stale[fam] = !DaxIo.FamilyUsable(fam) && HasFiles(fam);
                if (stale[fam])
                    anyStale = true;
            }

            if (anyStale)
            {
                RequirePlatform();
                logFile = null;
                logOpen = false;
                try
                {
                string logPath = GamePath(LOG_NAME);
                try
                {
                    logFile = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    logOpen = true;
                    ioSuccess = true;
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                    {
                        SetIoError(ex);
                        logOpen = false;
                    }
                    else
                    {
                        throw;
                    }
                }

                writeNote = "";
                memNote = "";
                failNote = "";
                archiveName = "DATABLD";

                if (CanWrite())
                    RemoveFile(archiveName + ".$$$");

                RequirePlatform();
                Platform.Open(stale, writeNote == "");
                Progress("Please wait, constructing data files...");
                LogLine("Merged data archives, version " + C_Data.VERSION.ToString());

                builtCount = 0;
                keptCount = 0;
                failedCount = 0;
                crcTable = null;

                int need = 1024 + 2 * CHUNK_SIZE + 2 * MAX_BLOCKS * 14 + 1024;
                if (Platform.MaxAvailable < need)
                {
                    memNote = "not enough memory";
                }
                else
                {
                    try
                    {
                        crcTable = new TCrcTable();
                        bufA = new byte[CHUNK_SIZE];
                        bufB = new byte[CHUNK_SIZE];
                        sources = new TBlock[MAX_BLOCKS + 1];
                        blocks = new TBlock[MAX_BLOCKS + 1];
                        sourceList = new byte[SOURCES_SIZE];
                        remapDisk = new byte[MAX_REMAPS + 1];
                        remapFrom = new byte[MAX_REMAPS + 1];
                        remapTo = new byte[MAX_REMAPS + 1];
                        remapBlock = new ushort[MAX_REMAPS + 1];
                        InitCrcTable();
                    }
                    catch (OutOfMemoryException)
                    {
                        memNote = "not enough memory";
                    }
                }

                for (byte fam = 1; fam <= C_Data.FAMILIES; fam++)
                {
                    if (stale[fam])
                    {
                        RequirePlatform();
                        Platform.StartFamily(fam);
                        UpdateFamily(fam);
                        Platform.EndFamily(fam, failNote == null || failNote.Length == 0);
                    }
                    else if (HasFiles(fam))
                    {
                        LogLine(DataArc.MergedName(fam) + ".DAX up to date");
                        keptCount++;
                    }
                }

                if (crcTable != null)
                {
                    blocks = null;
                    sources = null;
                    bufB = null;
                    bufA = null;
                    crcTable = null;
                }

                LogLine("Done: " + builtCount.ToString() + " built, " + keptCount.ToString() + " up to date, " + failedCount.ToString() + " failed.");

                if (logOpen)
                {
                    try
                    {
                        logFile.Close();
                        ioSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                        {
                            SetIoError(ex);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    n = lastError;
                    logOpen = false;
                }

                RequirePlatform();
                Platform.ClearBottomLine();
                Platform.Finish(failedCount > 0);
                DaxIo.ResetArchiveCache();
                }
                finally
                {
                    if (logFile != null)
                    {
                        try
                        {
                            logFile.Dispose();
                        }
                        catch (Exception ex)
                        {
                            if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException)
                            {
                                SetIoError(ex);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    logFile = null;
                    logOpen = false;
                    sources = null;
                    blocks = null;
                    bufA = null;
                    bufB = null;
                    crcTable = null;
                    DaxIo.ResetArchiveCache();
                }
            }
        }
    }
}
