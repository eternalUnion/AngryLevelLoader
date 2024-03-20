using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace RudeLevelScripts
{
    public class RudeMapVarHandler : MonoBehaviour
    {
        [Header("This is the id of the file to register the list of MapVars below with. Keep in mind, fileID's are not bundle or level specific.")]
        public string fileID;

        [Space(10)]
        [Header("Every MapVar key you list here will be registered to be set/read from the provided fileID. For any MapVarSetter components that use keys in this list, their persistence setting will be ignored and it will only read/write to the provided fileID.")]
        public List<string> varList;

        const int MAX_FILE_NAME_LENGTH = 48;

        //This will validate on running clients, so even if the mapmaker sets an invalid fileID, it should be caught before it causes issues.
        public bool IsValid()
        {
            //No empty fileID.
            if (string.IsNullOrEmpty(fileID) || string.IsNullOrWhiteSpace(fileID))
            {
                Debug.LogError($"({name}) {nameof(RudeMapVarHandler)}.{nameof(fileID)} is empty, null, or whitespace.");
                return false;
            }

            //Check for invalid characters in the fileID
            if (fileID.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                Debug.LogError($"({name}) {nameof(RudeMapVarHandler)}.{nameof(fileID)} contains invalid characters.");
                return false;
            }

            //Limit the fileID to 64 characters
            if(fileID.Length > MAX_FILE_NAME_LENGTH)
            {
                Debug.LogError($"({name}) {nameof(RudeMapVarHandler)}.{nameof(fileID)} is too long. Max length is {MAX_FILE_NAME_LENGTH} characters.");
                return false;
            }

            return true;
        }

        //Runs in editor on field change should prevent most invalid setups.
        private void OnValidate()
        {
            //Check for invalid characters in the fileID
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (fileID.IndexOfAny(invalidChars) != -1)
            {
                Debug.LogError("RudeMapVarHandler: fileID of RudeMapVarHandler contains invalid characters.");

                //Purge invalid characters from the fileID
                for (int i = 0; i < invalidChars.Length; i++)
                {
                    while (fileID.IndexOf(invalidChars[i]) != -1)
                        fileID = fileID.Remove(fileID.IndexOf(invalidChars[i]), 1);
                }
            }

            //Limit the fileID to 64 characters
            if (fileID.Length > MAX_FILE_NAME_LENGTH)
            {
                fileID = fileID.Substring(0, MAX_FILE_NAME_LENGTH);
            }
        }
    }
}
