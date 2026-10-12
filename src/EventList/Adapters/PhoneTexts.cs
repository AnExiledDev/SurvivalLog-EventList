using System;
using System.Collections.Generic;
using EventList.Core;
using GameCore.HotUpdate.Battle.Logic;

namespace EventList.Adapters;

/// <summary>
/// Reads the event texts in the player's phone for the event badge. Read only: nothing in the game is written.
/// Counts the conversations the game's own unread badge counts (PhoneSMSManager.GetUnreadCounts): none while the
/// phone is locked, and none from a contact who died or was lost. A read that fails hides the badge and is logged once.
/// </summary>
internal sealed class PhoneTexts
{
    private readonly Action<string> _log;
    private bool _isFailureLogged;

    public PhoneTexts(Action<string> log)
    {
        _log = log;
    }

    public PhoneBadge Read()
    {
        try
        {
            return PhoneBadge.Of(EventTexts());
        }
        catch (Exception ex)
        {
            if (!_isFailureLogged) _log("could not read the phone's texts, so the event badge is hidden (logged once): " + ex.Message);

            _isFailureLogged = true;

            return PhoneBadge.None;
        }
    }

    private static IEnumerable<EventText> EventTexts()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var phone = world?._PhoneSMSManager;
        var conversations = phone == null || phone.IsLocked ? null : phone._conversations;

        if (world == null || conversations == null) yield break;

        var strangers = world._StrangerSupportManager;

        for (var c = 0; c < conversations.Count; c++)
        {
            var conversation = conversations[c];
            var messages = conversation?.Messages;

            if (conversation == null || messages == null || conversation.IsContactDead) continue;
            if (strangers != null && strangers.IsLost(conversation.ContactId)) continue;

            for (var m = 0; m < messages.Count; m++)
            {
                var message = messages[m];

                if (message != null && message.IsEvent) yield return new EventText(message.EventState, message.TaskDeadlineGameSec);
            }
        }
    }
}
