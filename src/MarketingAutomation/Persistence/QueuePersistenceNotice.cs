using System.Collections;
using MarketingAutomation.GameIntegration.UI;
using UnityEngine;

namespace MarketingAutomation.Persistence;

public sealed class QueuePersistenceNotice : MonoBehaviour
{
    internal void Show(GUI_Main manager, string message) => StartCoroutine(Display(manager, message));

    private IEnumerator Display(GUI_Main manager, string message)
    {
        yield return null; // Let the native save/load UI finish its own callbacks.
        if (manager)
        {
            var ui = new NativeGameUi(manager);
            ui.ShowMessage(message);
            ui.MessageDialog.transform.SetAsLastSibling();
        }

        Destroy(this);
    }
}
