using UnityEngine;
using UnityEngine.UI;

public class EscapeBackButtonHandler : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject[] activePanels;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (backButton == null || !IsAnyPanelActive())
            return;

        backButton.onClick.Invoke();
    }

    private bool IsAnyPanelActive()
    {
        if (activePanels == null || activePanels.Length == 0)
            return true;

        for (int i = 0; i < activePanels.Length; i++)
        {
            if (activePanels[i] != null && activePanels[i].activeInHierarchy)
                return true;
        }

        return false;
    }
}
