//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 4.0.0
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace Assimp {

public class ExportProperties : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal ExportProperties(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(ExportProperties obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~ExportProperties() {
    Dispose(false);
  }

  public void Dispose() {
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          assimp_swigPINVOKE.delete_ExportProperties(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }

  public ExportProperties() : this(assimp_swigPINVOKE.new_ExportProperties__SWIG_0(), true) {
  }

  public ExportProperties(ExportProperties other) : this(assimp_swigPINVOKE.new_ExportProperties__SWIG_1(ExportProperties.getCPtr(other)), true) {
    if (assimp_swigPINVOKE.SWIGPendingException.Pending) throw assimp_swigPINVOKE.SWIGPendingException.Retrieve();
  }

  public bool SetPropertyInteger(string szName, int iValue) {
    bool ret = assimp_swigPINVOKE.ExportProperties_SetPropertyInteger(swigCPtr, szName, iValue);
    return ret;
  }

  public bool SetPropertyBool(string szName, bool value) {
    bool ret = assimp_swigPINVOKE.ExportProperties_SetPropertyBool(swigCPtr, szName, value);
    return ret;
  }

  public bool SetPropertyFloat(string szName, float fValue) {
    bool ret = assimp_swigPINVOKE.ExportProperties_SetPropertyFloat(swigCPtr, szName, fValue);
    return ret;
  }

  public bool SetPropertyString(string szName, string sValue) {
    bool ret = assimp_swigPINVOKE.ExportProperties_SetPropertyString(swigCPtr, szName, sValue);
    if (assimp_swigPINVOKE.SWIGPendingException.Pending) throw assimp_swigPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public bool SetPropertyMatrix(string szName, aiMatrix4x4 sValue) {
    bool ret = assimp_swigPINVOKE.ExportProperties_SetPropertyMatrix(swigCPtr, szName, aiMatrix4x4.getCPtr(sValue));
    if (assimp_swigPINVOKE.SWIGPendingException.Pending) throw assimp_swigPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public int GetPropertyInteger(string szName, int iErrorReturn) {
    int ret = assimp_swigPINVOKE.ExportProperties_GetPropertyInteger__SWIG_0(swigCPtr, szName, iErrorReturn);
    return ret;
  }

  public int GetPropertyInteger(string szName) {
    int ret = assimp_swigPINVOKE.ExportProperties_GetPropertyInteger__SWIG_1(swigCPtr, szName);
    return ret;
  }

  public bool GetPropertyBool(string szName, bool bErrorReturn) {
    bool ret = assimp_swigPINVOKE.ExportProperties_GetPropertyBool__SWIG_0(swigCPtr, szName, bErrorReturn);
    return ret;
  }

  public bool GetPropertyBool(string szName) {
    bool ret = assimp_swigPINVOKE.ExportProperties_GetPropertyBool__SWIG_1(swigCPtr, szName);
    return ret;
  }

  public float GetPropertyFloat(string szName, float fErrorReturn) {
    float ret = assimp_swigPINVOKE.ExportProperties_GetPropertyFloat__SWIG_0(swigCPtr, szName, fErrorReturn);
    return ret;
  }

  public float GetPropertyFloat(string szName) {
    float ret = assimp_swigPINVOKE.ExportProperties_GetPropertyFloat__SWIG_1(swigCPtr, szName);
    return ret;
  }

  public string GetPropertyString(string szName, string sErrorReturn) {
    string ret = assimp_swigPINVOKE.ExportProperties_GetPropertyString__SWIG_0(swigCPtr, szName, sErrorReturn);
    if (assimp_swigPINVOKE.SWIGPendingException.Pending) throw assimp_swigPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public string GetPropertyString(string szName) {
    string ret = assimp_swigPINVOKE.ExportProperties_GetPropertyString__SWIG_1(swigCPtr, szName);
    return ret;
  }

  public aiMatrix4x4 GetPropertyMatrix(string szName, aiMatrix4x4 sErrorReturn) {
    aiMatrix4x4 ret = new aiMatrix4x4(assimp_swigPINVOKE.ExportProperties_GetPropertyMatrix__SWIG_0(swigCPtr, szName, aiMatrix4x4.getCPtr(sErrorReturn)), true);
    if (assimp_swigPINVOKE.SWIGPendingException.Pending) throw assimp_swigPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public aiMatrix4x4 GetPropertyMatrix(string szName) {
    aiMatrix4x4 ret = new aiMatrix4x4(assimp_swigPINVOKE.ExportProperties_GetPropertyMatrix__SWIG_1(swigCPtr, szName), true);
    return ret;
  }

  public bool HasPropertyInteger(string szName) {
    bool ret = assimp_swigPINVOKE.ExportProperties_HasPropertyInteger(swigCPtr, szName);
    return ret;
  }

  public bool HasPropertyBool(string szName) {
    bool ret = assimp_swigPINVOKE.ExportProperties_HasPropertyBool(swigCPtr, szName);
    return ret;
  }

  public bool HasPropertyFloat(string szName) {
    bool ret = assimp_swigPINVOKE.ExportProperties_HasPropertyFloat(swigCPtr, szName);
    return ret;
  }

  public bool HasPropertyString(string szName) {
    bool ret = assimp_swigPINVOKE.ExportProperties_HasPropertyString(swigCPtr, szName);
    return ret;
  }

  public bool HasPropertyMatrix(string szName) {
    bool ret = assimp_swigPINVOKE.ExportProperties_HasPropertyMatrix(swigCPtr, szName);
    return ret;
  }

}

}
