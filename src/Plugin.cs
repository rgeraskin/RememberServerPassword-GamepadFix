using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace RememberServerPasswordGamepadFix
{
    // A temporary fix for MagiCorp's Remember Server Password until its own fix is released:
    // https://github.com/iMagic16/RememberServerPassword/pull/3
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(TargetGuid)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "rgeraskin.valheim.rememberserverpassword.gamepadfix";
        public const string Name = "RememberServerPassword Gamepad Fix";
        public const string Version = "0.1.0";
        internal const string TargetGuid = "magicorp.valheim.rememberserverpassword";

        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            new Harmony(Guid).PatchAll();
        }

        internal static void Log(string message) => Instance.Logger.LogInfo(message);
        internal static void Warn(string message) => Instance.Logger.LogWarning(message);
    }

    // Must run after MagiCorp's own SetupGui postfix, which is what clones the button.
    [HarmonyPatch(typeof(FejdStartup), "SetupGui")]
    internal static class SetupGuiPatch
    {
        [HarmonyAfter(Plugin.TargetGuid)]
        private static void Postfix(FejdStartup __instance) => QuickConnectNavigation.Link(__instance);
    }

    internal static class QuickConnectNavigation
    {
        private const string QuickConnectObjectName = "MagiCorp_RememberServerPassword_QuickConnect";

        // The main menu buttons form an explicit up/down Navigation chain (vanilla also wires
        // selectOnRight to the merch button), and FejdStartup caches m_menuButtons in Awake for
        // arrow-key navigation. A button cloned into the menu later is in neither, so both the
        // gamepad and the arrow keys jump straight over it. Splice it into both.
        internal static void Link(FejdStartup startup)
        {
            try
            {
                GameObject menuList = AccessTools.Field(typeof(FejdStartup), "m_menuList")?.GetValue(startup) as GameObject;
                var buttonsField = AccessTools.Field(typeof(FejdStartup), "m_menuButtons");
                Button[] menuButtons = buttonsField?.GetValue(startup) as Button[];
                if (menuList == null || menuButtons == null)
                    return;

                Button quick = null;
                foreach (Button candidate in menuList.GetComponentsInChildren<Button>(true))
                {
                    if (candidate.gameObject.name == QuickConnectObjectName)
                        quick = candidate;
                }
                if (quick == null)
                    return;
                Button reference = Array.Find(menuButtons, b => b != null && b != quick);
                if (reference != null)
                    CopySelectionVisuals(reference, quick);

                // A disabled Quick Connect ("No recent server") is better left out of the chain.
                if (quick == null || !quick.gameObject.activeSelf || !quick.interactable || Array.IndexOf(menuButtons, quick) >= 0)
                    return;

                // The vanilla button closest above Quick Connect becomes its predecessor.
                float quickY = quick.transform.position.y;
                int aboveIndex = -1;
                for (int i = 0; i < menuButtons.Length; i++)
                {
                    if (menuButtons[i] == null)
                        continue;
                    float y = menuButtons[i].transform.position.y;
                    if (y > quickY && (aboveIndex < 0 || y < menuButtons[aboveIndex].transform.position.y))
                        aboveIndex = i;
                }
                Button above = aboveIndex >= 0 ? menuButtons[aboveIndex] : null;

                var ordered = new List<Button>(menuButtons);
                ordered.Insert(aboveIndex + 1, quick);
                buttonsField.SetValue(startup, ordered.ToArray());

                Navigation nav = quick.navigation;
                if (nav.mode == Navigation.Mode.Explicit)
                {
                    // Take over whatever `above` pointed down to, so a wrap-around chain stays intact.
                    Selectable next = above != null
                        ? above.navigation.selectOnDown
                        : (aboveIndex + 2 < ordered.Count ? ordered[aboveIndex + 2] : null);
                    nav.selectOnUp = above;
                    nav.selectOnDown = next;
                    quick.navigation = nav;

                    if (above != null)
                    {
                        Navigation aboveNav = above.navigation;
                        aboveNav.selectOnDown = quick;
                        above.navigation = aboveNav;
                    }
                    if (next != null && next.navigation.selectOnUp == above)
                    {
                        Navigation nextNav = next.navigation;
                        nextNav.selectOnUp = quick;
                        next.navigation = nextNav;
                    }
                }

                Plugin.Log($"Linked Quick Connect into menu navigation: mode={nav.mode}, " +
                           $"up={Name(nav.selectOnUp)}, down={Name(nav.selectOnDown)}, keyboardIndex={aboveIndex + 1}");
            }
            catch (Exception ex)
            {
                Plugin.Warn("Could not link Quick Connect navigation: " + ex.Message);
            }
        }

        // MagiCorp replaces the cloned Button with a fresh one to drop the template's persistent
        // onClick, copying transition/colors/sprites but not animationTriggers. Vanilla menu
        // buttons highlight through an Animator, so the default trigger names never match.
        private static void CopySelectionVisuals(Button reference, Button quick)
        {
            AnimationTriggers src = reference.animationTriggers;
            quick.animationTriggers = new AnimationTriggers
            {
                normalTrigger = src.normalTrigger,
                highlightedTrigger = src.highlightedTrigger,
                pressedTrigger = src.pressedTrigger,
                selectedTrigger = src.selectedTrigger,
                disabledTrigger = src.disabledTrigger,
            };
            quick.transition = reference.transition;
            quick.colors = reference.colors;
            quick.spriteState = reference.spriteState;
        }

        private static string Name(Component c) => c != null ? c.gameObject.name : "<none>";
    }
}
