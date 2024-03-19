using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RudeLevelScripts
{
    public class RudeMapVarHandler : MonoBehaviour
    {
        public string fileID;

        public List<string> varList;

        //I was going to add a reference to the linked AngryMapVarHandler so map makers can do operations to the MapVarHandler if needed,
        //but this dll doesnt reference the main dll so I have opted to leave it for now.
    }
}
