﻿using System;
using LuaInterface;
using SLua;
using System.Collections.Generic;
public class Lua_FairyGUI_GearLook : LuaObject {
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int constructor(IntPtr l) {
		try {
			FairyGUI.GearLook o;
			FairyGUI.GObject a1;
			checkType(l,2,out a1);
			o=new FairyGUI.GearLook(a1);
			pushValue(l,true);
			pushValue(l,o);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int Apply(IntPtr l) {
		try {
			FairyGUI.GearLook self=(FairyGUI.GearLook)checkSelf(l);
			self.Apply();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int UpdateState(IntPtr l) {
		try {
			FairyGUI.GearLook self=(FairyGUI.GearLook)checkSelf(l);
			self.UpdateState();
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static public int get_tweener(IntPtr l) {
		try {
			FairyGUI.GearLook self=(FairyGUI.GearLook)checkSelf(l);
			pushValue(l,true);
			pushValue(l,self.tweener);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	static public void reg(IntPtr l) {
		getTypeTable(l,"FairyGUI.GearLook");
		addMember(l,Apply);
		addMember(l,UpdateState);
		addMember(l,"tweener",get_tweener,null,true);
		createTypeMetatable(l,constructor, typeof(FairyGUI.GearLook),typeof(FairyGUI.GearBase));
	}
}
