using UnityEngine;
using System.Collections;
using FairyGUI;

public class MyGLoader : GLoader {

    protected override void LoadExternal()
    {
        base.LoadExternal();

        IconManager.inst.LoadIcon(this.url, OnLoadSuccess, OnLoadFail);
    }

    protected override void FreeExternal(NTexture texture)
    {
        base.FreeExternal(texture);
        texture.refCount--;
    }

    void OnLoadSuccess(NTexture texture)
    {
        if (string.IsNullOrEmpty(this.url))
        {
            return;
        }
        this.onExternalLoadSuccess(texture);
    }

    void OnLoadFail(string error)
    {
        Debug.Log("load " + this.url + " , failed : " + error);
        this.onExternalLoadFailed();
    }
}
