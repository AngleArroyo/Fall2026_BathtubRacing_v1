
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialPageManager : MonoBehaviour
{
    [Header("Tutorial Pages")]
    public GameObject[] pages;

    private int currentPage = 0;

    void Start()
    {
        // Start tutorial on Page 1
        ShowPage(0);
    }

    // Next button: Go to the next tutorial page
    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            ShowPage(currentPage + 1);
        }
    }

    // Back button: Return to previous page
    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            ShowPage(currentPage - 1);
        }
        else
        {
            BackToMainMenu();
        }
    }

    // Main Menu button: Return to MainMenu scene
    public void BackToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        SceneManager.LoadScene("MainMenu");
    }

    // Display selected tutorial page
    private void ShowPage(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogError("No tutorial pages assigned!");
            return;
        }

        if (pageIndex < 0 || pageIndex >= pages.Length)
            return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == pageIndex);
            }
        }

        currentPage = pageIndex;
    }
}
