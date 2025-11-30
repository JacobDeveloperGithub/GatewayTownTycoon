using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ScenarioSelect : MonoBehaviour {
    private UIDocument _document;

    private string[] _nextScenes = new string[3]{"ScenarioOne", "ScenarioTwo", "ScenarioThree"};

    private void OnEnable() {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;

        for (int i = 0; i < _nextScenes.Length; i++) {
           int val = i;
           root.Query<Button>().Build().AtIndex(i).RegisterCallback<ClickEvent>(ctx => SceneManager.LoadScene(_nextScenes[val])); 
        }
    }
}