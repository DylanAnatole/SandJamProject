using System;
using UnityEngine;
namespace SandJamTest
{
    // A fresh definition for each load prevents editor/test mutations leaking between sessions.
    public static class LevelDataManager
    {
        public static LevelData Load(TextAsset asset)
        {
            if(!asset)throw new ArgumentNullException("asset","Level JSON is missing");
            try
            {
                var data=JsonUtility.FromJson<LevelData>(asset.text);
                SandGame.Validate(data);
                if(data.uiDivider<=0)throw new ArgumentException("uiDivider must be positive");
                if(data.rowCount!=112 || data.columnCount!=84 || data.laneData.Length!=3)throw new ArgumentException("Current scene layout requires an 84 x 112 board and three lanes");
                if(data.gridSlotNeedAmmoCount!=null && (data.gridSlotNeedAmmoCount.Length>5 || Array.Exists(data.gridSlotNeedAmmoCount,x=>x<0)))throw new ArgumentException("Invalid five-slot thresholds");
                return data;
            }
            catch(Exception e){throw new ArgumentException("Cannot load level '"+asset.name+"': "+e.Message,e);}
        }
    }
}
