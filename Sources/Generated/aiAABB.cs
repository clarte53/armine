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

public class aiAABB : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal aiAABB(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(aiAABB obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~aiAABB() {
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
          assimp_swigPINVOKE.delete_aiAABB(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }

  public aiVector3D mMin {
    set {
      assimp_swigPINVOKE.aiAABB_mMin_set(swigCPtr, aiVector3D.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = assimp_swigPINVOKE.aiAABB_mMin_get(swigCPtr);
      aiVector3D ret = (cPtr == global::System.IntPtr.Zero) ? null : new aiVector3D(cPtr, false);
      return ret;
    } 
  }

  public aiVector3D mMax {
    set {
      assimp_swigPINVOKE.aiAABB_mMax_set(swigCPtr, aiVector3D.getCPtr(value));
    } 
    get {
      global::System.IntPtr cPtr = assimp_swigPINVOKE.aiAABB_mMax_get(swigCPtr);
      aiVector3D ret = (cPtr == global::System.IntPtr.Zero) ? null : new aiVector3D(cPtr, false);
      return ret;
    } 
  }

  public aiAABB() : this(assimp_swigPINVOKE.new_aiAABB__SWIG_0(), true) {
  }

  public aiAABB(aiVector3D min, aiVector3D max) : this(assimp_swigPINVOKE.new_aiAABB__SWIG_1(aiVector3D.getCPtr(min), aiVector3D.getCPtr(max)), true) {
    if (assimp_swigPINVOKE.SWIGPendingException.Pending) throw assimp_swigPINVOKE.SWIGPendingException.Retrieve();
  }

}

}
