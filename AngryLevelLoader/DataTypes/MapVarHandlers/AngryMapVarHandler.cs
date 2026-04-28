using RudeLevelScripts;

namespace AngryLevelLoader.DataTypes.MapVarHandlers
{
	//This will just hold onto the RudeMapVarHandler so it can be accessed for any reasons
	internal class AngryMapVarHandler : PersistentMapVarHandler
    {
        public RudeMapVarHandler RudeMapVarHandler { get; private set; }

        public AngryMapVarHandler(string filePath, RudeMapVarHandler rudeMapVarHandler) : base(filePath)
        {
            RudeMapVarHandler = rudeMapVarHandler;
        }

    }
}
