using UnityEngine;
using System.Collections;
using SLua;

public class AppMain : MonoBehaviour {

    private LuaSvr lua_svr;
    private string luaworkPathOrigin = "Assets/LuaWork/";

	// Use this for initialization
	void Start () {
        // 创建一个已经注入UnityEngine的状态机对象
        lua_svr = new LuaSvr();
        // 通过Resources文件夹下的main.txt(lua)文件内的main函数启动程序
        // 当然也可以通过修改LuaState.loaderDelegate来修改默认的路径
        string luaFilePath = luaworkPathOrigin + "Main.lua";
        lua_svr.start(luaFilePath);
	}
	
	// Update is called once per frame
	void Update () {
	    
	}
}
