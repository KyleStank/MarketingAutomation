using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarketingAutomation.UI.Controls;

public sealed class SearchInputFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    internal GUI_Main MenuManager;
    internal InputField Input;
    private bool ownsFocus;
    private bool releasePending;

    private void Start() => Input.onEndEdit.AddListener(_ => releasePending = true);

    public void OnSelect(BaseEventData eventData)
    {
        ownsFocus = true;
        releasePending = false;
        MenuManager.SelectInputField();
    }

    public void OnDeselect(BaseEventData eventData) => releasePending = true;

    private void LateUpdate()
    {
        // InputField activates in LateUpdate and can end editing (including Escape)
        // without a selection change. Never release on its initial unfocused frame.
        if (releasePending)
        {
            Release();
        }
        else if (Input.isFocused && !ownsFocus)
        {
            ownsFocus = true;
            MenuManager.SelectInputField();
        }
    }

    private void Release()
    {
        if (ownsFocus && MenuManager)
        {
            MenuManager.DeselectInputField();
        }

        ownsFocus = false;
        releasePending = false;
    }

    private void OnDisable() => Release();
}
