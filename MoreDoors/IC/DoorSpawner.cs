﻿using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using MoreDoors.Data;
using UnityEngine;

namespace MoreDoors.IC
{
    public class DoorNameMarker : MonoBehaviour
    {
        public string DoorName;
    }

    public static class DoorSpawner
    {

        private static FsmVar NewStringVar(string text)
        {
            FsmVar ret = new(typeof(string));
            ret.stringValue = text;
            return ret;
        }

        private static void SetupConversationControl(PlayMakerFSM fsm, DoorData data, bool left)
        {
            fsm.GetState("Check Key").GetFirstActionOfType<PlayerDataBoolTest>().boolName = data.PDKeyName;
            fsm.GetState("Send Text").GetFirstActionOfType<CallMethodProper>().parameters[0] = NewStringVar(data.KeyPromptId);
            fsm.GetState("No Key").GetFirstActionOfType<CallMethodProper>().parameters[0] = NewStringVar(data.NoKeyPromptId);
            fsm.GetState("Open").AddFirstAction(new Lambda(() => Preloader.Instance.ReparentDoor(fsm.gameObject, left)));

            var setters = fsm.GetState("Yes").GetActionsOfType<SetPlayerDataBool>();
            setters[0].boolName = MoreDoorsModule.EmptyBoolName;
            setters[1].boolName = data.PDDoorOpenedName;
        }

        private static void SetupNpcControlOnRight(PlayMakerFSM fsm)
        {
            fsm.FsmVariables.FindFsmBool("Hero Always Left").Value = true;
            fsm.FsmVariables.FindFsmBool("Hero Always Right").Value = false;
        }

        private static readonly Color darkDoorColor = new(0.2647f, 0.2647f, 0.2647f);

        public static void SpawnDoor(SceneManager sm, string doorName, bool left)
        {
            var data = DoorData.Get(doorName);
            var gameObj = Preloader.Instance.NewDoor();
            var renderer = gameObj.GetComponent<SpriteRenderer>();
            renderer.sprite = new EmbeddedSprite($"Doors.{data.Door.Sprite}").Value;

            SetupConversationControl(gameObj.LocateMyFSM("Conversation Control"), data, left);

            var loc = left ? data.Door.LeftLocation : data.Door.RightLocation;
            gameObj.transform.position = new(loc.X, loc.Y, gameObj.transform.position.z);
            if (!left)
            {
                gameObj.transform.rotation = new(0, 180, 0, 0);
                SetupNpcControlOnRight(gameObj.LocateMyFSM("npc_control"));
            }

            gameObj.name = $"{data.CamelCaseName} Door";
            gameObj.AddComponent<DoorNameMarker>().DoorName = doorName;

            var promptMarker = gameObj.FindChild("Prompt Marker");
            promptMarker.transform.localPosition = new(0.7f, 0.77f, 0.206f);
            promptMarker.AddComponent<DeactivateInDarknessWithoutLantern>().enabled = true;

            // TODO: Establish an ordering between mod initializations on scene load.
            // Implement a constraints mod that supports this without the need for linking.
            if (sm.darknessLevel == 2 && !PlayerData.instance.GetBool(nameof(PlayerData.instance.hasLantern)))
            {
                // Why is this so finicky
                GameObject.Destroy(promptMarker);
                GameObject.Destroy(gameObj.LocateMyFSM("npc_control"));
                renderer.color = darkDoorColor;
            }

            gameObj.SetActive(true);
        }

    }
}
