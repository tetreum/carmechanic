using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public static Menu _instance;

    public List<GameObject> Menus;

    public static Menu Instance => _instance;

    private void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
        DontDestroyOnLoad(GameObject.Find("EventSystem"));
    }

    private void Start()
    {
        _instance = this;
        OnLevelWasLoaded(0);
    }

    public void OnLevelWasLoaded(int level)
    {
        switch (level)
        {
            case 0: // Main
                showPanel("MainPanel");
                break;
            default: // Test
                hidePanel("MainPanel");
                showPanel("EditorModePanel");
                Cursors.setLocked();
                break;
        }

        // always disable DevCamera
        try
        {
            GameObject.Find("DevCamera").SetActive(false);
        }
        catch (Exception)
        {
        }
    }

    public GameObject getPanel(string name)
    {
        foreach (var panel in Menus)
            if (panel.name == name)
                return panel;
        throw new UnityException("UI Panel " + name + " not found");
    }

    public void togglePanel(string name)
    {
        var panel = getPanel(name);

        panel.SetActive(!panel.activeSelf);
    }

    public GameObject showPanel(string name, bool hidePanels = true)
    {
        if (hidePanels) hideAllPanels();

        var panel = getPanel(name);
        panel.SetActive(true);

        return panel;
    }

    public void hidePanel(string name)
    {
        foreach (var panel in Menus)
            if (panel.name == name)
                panel.SetActive(false);
    }

    public void hideAllPanels()
    {
        foreach (var panel in Menus) panel.SetActive(false);
    }

    /*
     * We place this here since SmartphonePanel gets disabled when taking a screenshot
     */
    public void afterScreenshot(string file)
    {
        StartCoroutine(showUI(file));
    }

    private IEnumerator showUI(string file)
    {
        yield return new WaitForSeconds(0.2f);

        Debug.Log("Screenshot saved as " + file);

        Instance.showPanel("SmartphonePanel", false);
        Instance.showPanel("PlayerPanel", false);
    }
}