using System;
using LuaInterface;
using SLua;
using System.Collections.Generic;
public class Lua_FairyGUI_CopyPastePatch : LuaObject {
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int constructor(IntPtr l) {
		try {
			FairyGUI.CopyPastePatch o;
			o=new FairyGUI.CopyPastePatch();
			pushValue(l,true);
			pushValue(l,o);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Apply_s(IntPtr l) {
		try {
			FairyGUI.CopyPastePatch.Apply();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	static public void reg(IntPtr l) {
		getTypeTable(l,"FairyGUI.CopyPastePatch");
		addMember(l,Apply_s);
		createTypeMetatable(l,constructor, typeof(FairyGUI.CopyPastePatch));
	}
}
