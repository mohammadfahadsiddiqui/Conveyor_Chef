// using UnityEngine;
// using UnityEngine.SceneManagement;
// using Watermelon.BusStop;

// namespace Watermelon
// {
//     public class settinghome : SettingsButtonBase
//     {
//         public override bool IsActive()
//         {
//             // Always show home button
//             return true;
//         }

//         public override void OnClick()
//         {
//             // Play button sound
//             AudioController.PlaySound(AudioController.Sounds.buttonSound);

//             // Reset level selection mode flag
//             LevelSave levelSave = SaveController.GetSaveObject<LevelSave>("level");
//             if (levelSave != null)
//             {
//                 levelSave.isPlayingFromLevelSelection = false;
//                 SaveController.MarkAsSaveIsRequired();
//                 SaveController.Save(true);
//             }

//             Debug.Log("[SettingHome] Going to Level Selection");

//             // Load level selection scene
//             SceneManager.LoadScene("LevelSelection");
//         }
//     }
// }