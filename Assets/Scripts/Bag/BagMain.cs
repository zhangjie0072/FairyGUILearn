using UnityEngine;
using System.Collections;
using FairyGUI;

public class BagMain : MonoBehaviour {

    GComponent _mainView;
    BagWindow _bagWindow;

    void Awake()
    {
        UIObjectFactory.SetLoaderExtension(typeof(MyGLoader));
    }

	// Use this for initialization
	void Start () {
        Application.targetFrameRate = 60;
        GRoot.inst.SetContentScaleFactor(1136, 640);
        _mainView = this.GetComponent<UIPanel>().ui;
        if (_mainView != null)
        {
            _bagWindow = new BagWindow();
            _mainView.GetChild("bag_btn").onClick.Add(() =>
            {
                Debug.Log("bag btn clicked");
                _bagWindow.Show();
            });
        }
        
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
