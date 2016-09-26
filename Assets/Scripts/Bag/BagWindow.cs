using UnityEngine;
using System.Collections;
using FairyGUI;
using DG.Tweening;

public class BagWindow : Window {

    GList _list;

    public BagWindow()
    {

    }

    protected override void OnInit()
    {
        base.OnInit();

        this.contentPane = UIPackage.CreateObject("Bag", "BagWin").asCom;
        this.Center();
        this.modal = true;

        _list = this.contentPane.GetChild("list").asList;
        _list.onClickItem.Add(__clickItem);
    }

    protected override void DoShowAnimation()
    {
        base.DoShowAnimation();

        this.SetScale(0.1f, 0.1f);
        this.SetPivot(0.5f, 0.5f);
        this.TweenScale(new Vector2(1, 1), 0.3f).SetEase(Ease.OutQuad).OnComplete(this.OnShown);
    }

    protected override void DoHideAnimation()
    {
        base.DoHideAnimation();

        this.TweenScale(new Vector2(0.1f, 0.1f), 0.3f).SetEase(Ease.OutQuad).OnComplete(this.HideImmediately);
    }

    protected override void OnShown()
    {
        base.OnShown();

        for (int i = 0; i < 10; i++)
        {
            GButton button = _list.GetChildAt(i).asButton;
            button.icon = "i" + UnityEngine.Random.Range(0, 10);
            button.title = "" + UnityEngine.Random.Range(0, 100);
        }
    }

    void __clickItem(EventContext context)
    {
        GButton item = (GButton)context.data;
        this.contentPane.GetChild("selected_icon_pos").asLoader.url = item.icon;
        this.contentPane.GetChild("selected_icon_title").text = item.title;
    }
}
