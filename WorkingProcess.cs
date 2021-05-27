using log4net;
using MetadataExtractor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Directory = System.IO.Directory;

namespace NgxTranslationCreator
{
    public class WorkingProcess
    {

        private string searchDirectory;
        private string targetDirectory;
        private bool updateExisting;
        private bool onlyKeepExtractedTranslations;
        private Action resetRenamingThread;
        Action<double> updateProgressNumber;
        private double currentProgressNumber = 0;

        // private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog logger = LogManager.GetLogger(typeof(App));

        private readonly string sourceFileSearchingPatterns = System.Configuration.ConfigurationManager.AppSettings["sourceFileSearchingPatterns"];
        private readonly string translateSearchingPattern = System.Configuration.ConfigurationManager.AppSettings["translateSearchingPattern"];
        private readonly string languageFileNames = System.Configuration.ConfigurationManager.AppSettings["languageFileNames"];
        private readonly string jsonGroupIdentifier = System.Configuration.ConfigurationManager.AppSettings["jsonGroupIdentifier"];
        private readonly string jsonGroupStartIdentifier = System.Configuration.ConfigurationManager.AppSettings["jsonGroupStartIdentifier"];
        private readonly string jsonGroupEndIdentifier = System.Configuration.ConfigurationManager.AppSettings["jsonGroupEndIdentifier"];

        public WorkingProcess(string searchDirectory, string targetDirectory, bool updateExisting, bool onlyKeepExtractedTranslations, Action resetRenamingThread, Action<double> UpdateProgressNumber)
        {
            logger.Info("\n");
            logger.Info("Configure WorkingProcess");
            this.searchDirectory = searchDirectory;
            this.targetDirectory = targetDirectory;
            this.updateExisting = updateExisting;
            this.onlyKeepExtractedTranslations = onlyKeepExtractedTranslations;
            this.resetRenamingThread = resetRenamingThread;
            this.updateProgressNumber = UpdateProgressNumber;
            logger.Info(string.Format("Input:\n\t\t\t\t searchDirectory: \"{0}\";\n\t\t\t\t targetDirectory: \"{1}\";\n\t\t\t\t updateExisting: \"{2}\";\n\t\t\t\t onlyKeepExtractedTranslations: \"{3}\";", this.searchDirectory, this.targetDirectory = targetDirectory, this.updateExisting = updateExisting, this.onlyKeepExtractedTranslations));
            logger.Info(string.Format("Config:\n\t\t\t\t sourceFileSearchingPatterns: \"{0}\";\n\t\t\t\t translateSearchingPattern: \"{1}\";\n\t\t\t\t languageFileNames: \"{2}\";\n\t\t\t\t jsonGroupIdentifier: \"{3}\";\n\t\t\t\t jsonGroupStartIdentifier: \"{4}\";\n\t\t\t\t jsonGroupEndIdentifier: \"{5}\";", sourceFileSearchingPatterns, translateSearchingPattern, languageFileNames, jsonGroupIdentifier, jsonGroupStartIdentifier, jsonGroupEndIdentifier));

        }

        public void Process()
        {
            
            logger.Info("\n\t\t\t\t--------------------------------------------------------\n\t\t\t\tStarting WorkingProcess ...");
            try
            {
                this.currentProgressNumber = 0;
                updateProgressNumber(currentProgressNumber);
                increaseProgressNumber(1);

                // Create file-list for code scan
                List<string> FileList = new List<string>();
                List<string> BlackFileList = new List<string>();
                foreach (string searchPattern in sourceFileSearchingPatterns.Split('|'))
                {
                    FileList.AddRange(Directory.GetFiles(searchDirectory, searchPattern, SearchOption.AllDirectories));
                    BlackFileList.AddRange(Directory.GetFiles(targetDirectory, searchPattern, SearchOption.TopDirectoryOnly));
                }
                foreach (string blackFile in BlackFileList)
                {
                    //if(FileList.Contains(blackFile))
                    //{
                    FileList.Remove(blackFile);
                    //}
                }
                logger.Info(string.Format("Founded {0} Files for scan", FileList.Count));
                increaseProgressNumber(1);


                // Creating Translation File
                Dictionary<string, Dictionary<string, string>> translateDirectory = getAllTranslateIdentifier(FileList);

                List<Exception> errors = new List<Exception>();
                Dictionary<string, Dictionary<string, Dictionary<string, string>>> remainings = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                var languageFiles = languageFileNames.Split('|');
                double increaser = 60.0 / (double)(3.0 * ((languageFiles.Count() > 0) ? languageFiles.Count() : 1));
                foreach (string languageFile in languageFiles)
                {
                    logger.Info(string.Format("translation-file {0}: starting create", languageFile));
                    bool currentUpdateExsting = updateExisting;
                    string path = Path.Combine(this.targetDirectory, languageFile + ".json");
                    Dictionary<string, Dictionary<string, string>> existingFile = null;
                    if (currentUpdateExsting)
                    {
                        try
                        {
                            logger.Info(string.Format("translation-file {0}: start parsing existing file", languageFile));
                            existingFile = ParseExistingTranslationFile(path);
                            logger.Info(string.Format("translation-file {0}: existing file parsed", languageFile));
                            increaseProgressNumber(increaser);
                            remainings.Add(languageFile, updateExistingTranslationDictionary(translateDirectory, existingFile));
                            logger.Info(string.Format("translation-file {0}: updated translations with scanned", languageFile));
                            increaseProgressNumber(increaser);

                        }
                        catch (Exception e)
                        {
                            errors.Add(e);
                            var date = DateTime.Now;
                            path = Path.Combine(this.targetDirectory, languageFile + "_" + (date.ToString("yyyy_MM_dd__HH_mm_ss")) + ".json");
                            logger.Error(string.Format("translation-file {0}: parsing existing file failed. Create new File \"{1}.json\"", languageFile, date.ToString("yyyy_MM_dd__HH_mm_ss")));
                            currentUpdateExsting = false;
                        }
                    }
                    string FileToWrite = null;
                    if (currentUpdateExsting)
                    {
                        FileToWrite = generateWriteableLanguageJson(existingFile);
                        logger.Info(string.Format("translation-file {0}: translations decoded to json string", languageFile));
                    }
                    else
                    {
                        FileToWrite = generateWriteableLanguageJson(translateDirectory);
                    }
                    using (StreamWriter writer = File.CreateText(path))
                    {
                        writer.Write(FileToWrite);
                        logger.Info(string.Format("translation-file {0}: writed json string at \"{1}\"", languageFile, path));
                    }

                    increaseProgressNumber(increaser);
                }
                increaser = 15.0 / (double)((remainings.Count > 0) ? remainings.Count : 1);
                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> remaining in remainings)
                {
                    if (remaining.Value.Count > 0)
                    {
                        var path = Path.Combine(this.targetDirectory, remaining.Key + "_remaing_" + (DateTime.Now.ToString("yyyy_MM_dd__HH_mm_ss")) + ".log.json");
                        using (StreamWriter writer = File.CreateText(path))
                        {
                            writer.Write(generateWriteableLanguageJson(remaining.Value));
                            logger.Info(string.Format("translation-file {0}: writed additional translations of existing translation-file at \"{1}\"", remaining.Key, path));
                        }
                    }

                    increaseProgressNumber(increaser);
                }

                foreach (Exception e in errors)
                {
                    HandleError(e);
                }
                this.currentProgressNumber = 100;
                updateProgressNumber(currentProgressNumber);
            }
            catch (Exception e)
            {
                HandleError(e);
            }
            resetRenamingThread.Invoke();
        }

        [Obsolete("generateWriteableLanguageDictionary is deprecated, please use generateWriteableLanguageJson instead.")]
        private Dictionary<string, string> generateWriteableLanguageDictionary(Dictionary<string, Dictionary<string, string>> translationFile)
        {
            Dictionary<string, string> file = new Dictionary<string, string>();
            foreach (string componentKey in translationFile.Keys)
            {
                file.Add(jsonGroupStartIdentifier + componentKey, componentKey);
                foreach (KeyValuePair<string, string> translationPair in translationFile[componentKey])
                {
                    file.Add(translationPair.Key, translationPair.Value);
                }
                file.Add(jsonGroupEndIdentifier + componentKey, componentKey);
            }
            return file;
        }

        /// <summary>
        /// Creates a valid JSON-file as string, contains all translations structured
        /// </summary>
        /// <param name="translationDictionary"></param>
        /// <returns>JSON-file</returns>
        private string generateWriteableLanguageJson(Dictionary<string, Dictionary<string, string>> translationDictionary)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            int counterGroup = 0;
            foreach (string componentKey in translationDictionary.Keys)
            {
                // builder.AppendLine("\"" + jsonGroupStartIdentifier + componentKey + "\" : \"" + componentKey + "\",");
                builder.AppendLine(string.Format("\"{0}{1}\" : \"{1}\",", jsonGroupStartIdentifier, componentKey));
                foreach (KeyValuePair<string, string> translationPair in translationDictionary[componentKey])
                {
                    // builder.AppendLine("\"" + translationPair.Key + "\" : \"" + translationPair.Value + "\",");
                    builder.AppendLine(string.Format("\"{0}\" : \"{1}\",", translationPair.Key, translationPair.Value));
                }
                // builder.AppendLine("\"" + jsonGroupEndIdentifier + componentKey + "\" : \"" + componentKey + ((counterGroup < translationDictionary.Count - 1) ? "\"," : "\""));
                builder.AppendLine(string.Format("\"{0}{1}\" : \"{1}{2}", jsonGroupEndIdentifier, componentKey, ((counterGroup < translationDictionary.Count - 1) ? "\"," : "\"")));
                if (counterGroup < translationDictionary.Count - 1)
                {
                    builder.AppendLine("");
                    builder.AppendLine("");
                }
                counterGroup++;
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// Creates identification string from a file path
        /// </summary>
        /// <param name="fileName">file path</param>
        /// <returns>identification string</returns>
        private string getFileIdentifier(string fileName)
        {
            return jsonGroupIdentifier + fileName.Replace(searchDirectory + "\\", "").Replace("\\", ".");
        }

        /// <summary>
        /// Increases the Progress about increaseNumber
        /// </summary>
        /// <param name="increaseNumber">about value increase</param>
        private void increaseProgressNumber(double increaseNumber)
        {
            currentProgressNumber += increaseNumber;
            this.updateProgressNumber(this.currentProgressNumber);
        }


        /// <summary>
        /// Scan each file on file-list for translation-identifier and creates a translation-dictionary grouped by file-path
        /// </summary>
        /// <param name="FileList"></param>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, string>> getAllTranslateIdentifier(List<string> FileList)
        {
            double increaser = 23.0 / (double)(FileList.Count > 0 ? FileList.Count : 1);

            Dictionary<string, Dictionary<string, string>> translateDirectory = new Dictionary<string, Dictionary<string, string>>();
            List<string> knownKey = new List<string>();
            Regex re = new Regex(translateSearchingPattern);
            foreach (var fileName in FileList)
            {
                var fileIdentifier = getFileIdentifier(fileName);
                Dictionary<string, string> fileTranslateDic = new Dictionary<string, string>();
                foreach (var line in File.ReadLines(fileName))
                {
                    var matches = re.Matches(line);
                    foreach (Capture m in matches)
                    {
                        var value = m.Value.Replace("'", "");

                        if (fileTranslateDic.ContainsKey(value) == false)
                        {
                            // translation-identifier must be unique. Check if identifier already exists.
                            if (knownKey.Contains(value) == true)
                            {
                                // translation-identifier must be unique. Check if identifier already exists in translation-dictionary of current file and skip it
                                if (fileTranslateDic.ContainsKey(value + fileIdentifier) == false)
                                {
                                    fileTranslateDic.Add(value + fileIdentifier, "double_placeholder");
                                }
                            }
                            else
                            {
                                fileTranslateDic.Add(value, "placeholder");
                            }
                        }
                        knownKey.Add(value);
                    }
                    matches = null;
                }
                if (fileTranslateDic.Count > 0)
                {
                    translateDirectory.Add(fileIdentifier, fileTranslateDic);
                }

                increaseProgressNumber(increaser);
            }

            //Sort translation inside group alphabetical ascending
            Dictionary<string, Dictionary<string, string>> sorted = new Dictionary<string, Dictionary<string, string>>();
            int translationsCounter = 0;
            foreach (var e in translateDirectory)
            {
                sorted.Add(e.Key, new SortedDictionary<string, string>(e.Value).ToDictionary(obj => obj.Key, obj => obj.Value));
                translationsCounter += e.Value.Count;
            }
            logger.Info(string.Format("Founded overall {0} translations on scan", translationsCounter));
            return sorted;
        }

        /// <summary>
        /// Loads exsting translation file and parse it to an translations-dictionary grouped by name of file, where it used
        /// </summary>
        /// <param name="path">full file path</param>
        /// <returns>grouped translations-dictionary</returns>
        private Dictionary<string, Dictionary<string, string>> ParseExistingTranslationFile(string path)
        {
            Dictionary<string, Dictionary<string, string>> translationFile = new Dictionary<string, Dictionary<string, string>>();
            if (File.Exists(path) == false)
            {
                throw new FileNotFoundException("File at \"" + path + "\" not found");
            }
            else
            {
                // Load Json from file and parse as simple dictionary
                string fileString = File.ReadAllText(path);
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileString);
                fileString = null;
                var startIndexes = new List<int>();
                DictionaryEntry startEntry = null;
                bool handeldGroup = true;

                // group parsed dictionary by name of files, where translations used
                foreach (DictionaryEntry entry in json.Select((Entry, Index) => new DictionaryEntry(Index, Entry.Key, Entry.Value)))
                {
                    //Check current identifier is structural
                    if (entry.Value.StartsWith(jsonGroupIdentifier))
                    {
                        // Check current identifier indicate start of a group
                        if (entry.Key.StartsWith(jsonGroupStartIdentifier + jsonGroupIdentifier))
                        {
                            if (handeldGroup == false)
                            {
                                var message = string.Format("invalid structure at {0} -> {1}\ngroups must have an surrunding {2} and {3} value",
                                    entry.Key, entry.Value, jsonGroupStartIdentifier, jsonGroupEndIdentifier);
                                logger.Error(message);
                                throw new JsonException(message);
                            }
                            startEntry = entry;
                            handeldGroup = false;
                        }
                        // Check current identifier indicate end of a group
                        else if (entry.Key.StartsWith(jsonGroupEndIdentifier + jsonGroupIdentifier))
                        {
                            if (handeldGroup == true)
                            {
                                var message = string.Format("invalid structure: groups must have an surrunding {0} and {1} value", jsonGroupStartIdentifier, jsonGroupEndIdentifier);
                                logger.Error(message);
                                throw new JsonException(message);
                            }
                            if (entry.Value.Equals(startEntry?.Value) == false)
                            {
                                var message = string.Format("invalid structure at {0} -> {1}\ngroups must have an surrunding {2} and {3} value",
                                    entry.Key, entry.Value, jsonGroupStartIdentifier, jsonGroupEndIdentifier);
                                logger.Error(message);
                                throw new JsonException(message);
                            }
                            // Create new group, add all translation between start- and end-identifier
                            var normalKey = entry.Key.Replace(jsonGroupEndIdentifier, "");
                            translationFile.Add(
                                normalKey,
                                json.Where((Entry, Index) => Index > startEntry.Index && Index < entry.Index).ToDictionary(dict => dict.Key, dict => dict.Value)
                            );
                            startEntry = null;
                            handeldGroup = true;
                        }
                    }
                }
                if (handeldGroup == false)
                {
                    var message = string.Format("invalid structure: found invalid identifier: {0} -> {1}", startEntry.Key, startEntry.Value);
                    logger.Error(message);
                    throw new JsonException(message);
                }
            }
            return translationFile;
        }

        /// <summary>
        /// updates translationFile with code-extracted translations
        /// </summary>
        /// <param name="extractedTranslations">code-extracted translations</param>
        /// <param name="translationFile">translations of existing translation-file</param>
        /// <returns>grouped dictionary of additional translations of existing translation-file, not existing at code-extracted translations</returns>
        private Dictionary<string, Dictionary<string, string>> updateExistingTranslationDictionary(Dictionary<string, Dictionary<string, string>> extractedTranslations, Dictionary<string, Dictionary<string, string>> translationFile)
        {
            //Add not existing translation to translation-file
            Dictionary<string, Dictionary<string, string>> remaining = new Dictionary<string, Dictionary<string, string>>();
            foreach (string componentKey in extractedTranslations.Keys)
            {
                if (translationFile.ContainsKey(componentKey) == false)
                {
                    translationFile.Add(componentKey, new Dictionary<string, string>());
                }
                var translations = extractedTranslations[componentKey];
                var existingTranslations = translationFile[componentKey];
                foreach (KeyValuePair<string, string> translationPair in translations)
                {
                    if (existingTranslations.ContainsKey(translationPair.Key) == false)
                    {
                        existingTranslations.Add(translationPair.Key, translationPair.Value);
                    }
                }
            }

            //Check for additional translations of translation-file and copy for method return
            List<string> deleteGroups = new List<string>();
            Dictionary<string, List<string>> deleteTranslations = new Dictionary<string, List<string>>();
            foreach (string componentKey in translationFile.Keys)
            {
                //Check group existing
                if (extractedTranslations.ContainsKey(componentKey) == false)
                {
                    remaining.Add(componentKey, translationFile[componentKey]);
                    if (onlyKeepExtractedTranslations)
                    {
                        deleteGroups.Add(componentKey);
                    }
                }
                else
                {
                    var translations = translationFile[componentKey];
                    var extractedSubTranslations = extractedTranslations[componentKey];
                    foreach (KeyValuePair<string, string> translationPair in translations)
                    {
                        //Check group translations exising
                        if (extractedSubTranslations.ContainsKey(translationPair.Key) == false)
                        {
                            if (remaining.ContainsKey(componentKey) == false)
                            {
                                remaining.Add(componentKey, new Dictionary<string, string>());
                                if (onlyKeepExtractedTranslations)
                                {
                                    deleteTranslations.Add(componentKey, new List<string>());
                                }
                            }
                            remaining[componentKey].Add(translationPair.Key, translationPair.Value);
                            if (onlyKeepExtractedTranslations)
                            {
                                deleteTranslations[componentKey].Add(translationPair.Key);
                            }
                        }
                    }
                }
            }

            // Deleting additional translations
            if (onlyKeepExtractedTranslations)
            {
                foreach (string group in deleteGroups)
                {
                    translationFile.Remove(group);
                }
                foreach (string componentKey in deleteTranslations.Keys)
                {
                    var translations = deleteTranslations[componentKey];
                    foreach (string translationKey in translations)
                    {
                        translationFile[componentKey].Remove(translationKey);
                    }

                }
            }


            return remaining;
        }

        /// <summary>
        /// Handles catched Exceptions of Process
        /// </summary>
        /// <param name="exception"></param>
        private void HandleError(Exception exception)
        {
            logger.Error(exception.Message);
            if (exception is DirectoryNotFoundException || exception is FileNotFoundException)
            {
                MessageBox.Show(exception.Message, "Inputs invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            //else if (exception is JsonException)
            //{
            //    MessageBox.Show(exception.Message, "Exception while parsing translation-file", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
            else
            {
                MessageBox.Show(exception.ToString(), "Unkown error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            resetRenamingThread.Invoke();
        }



        /// <summary>
        /// Helper class for method "ParseExistingTranslationFile"
        /// </summary>
        protected class DictionaryEntry
        {
            public int Index;
            public string Key;
            public string Value;

            public DictionaryEntry(int index, string key, string value)
            {
                Index = index;
                Key = key;
                Value = value;
            }
        }
    }


}
