using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarketingAutomation.UI.Controls;

public sealed class SearchInputFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    internal GUI_Main MenuManager;
    internal InputField Input;
    private bool _ownsFocus;
    private bool _releasePending;

    private void Start() => Input.onEndEdit.AddListener(_ => _releasePending = true);

    public void OnSelect(BaseEventData eventData)
    {
        _ownsFocus = true;
        _releasePending = false;
        MenuManager.SelectInputField();
    }

    public void OnDeselect(BaseEventData eventData) => _releasePending = true;

    private void LateUpdate()
    {
        // InputField activates in LateUpdate and can end editing (including Escape)
        // without a selection change. Never release on its initial unfocused frame.
        if (_releasePending)
        {
            ReleaseGameInputFocus();
        }
        else if (Input.isFocused && !_ownsFocus)
        {
            _ownsFocus = true;
            MenuManager.SelectInputField();
        }
    }

    private void ReleaseGameInputFocus()
    {
        if (_ownsFocus && MenuManager)
            MenuManager.DeselectInputField();

        _ownsFocus = false;
        _releasePending = false;
    }

    private void OnDisable() => ReleaseGameInputFocus();
}
