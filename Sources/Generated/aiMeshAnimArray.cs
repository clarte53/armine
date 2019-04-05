//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.11
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace Assimp {

public class aiMeshAnimArray : global::System.IDisposable, Interface.Array<aiMeshAnim> {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal aiMeshAnimArray(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(aiMeshAnimArray obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~aiMeshAnimArray() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          assimp_swigPINVOKE.delete_aiMeshAnimArray(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public void Clear() {
    assimp_swigPINVOKE.aiMeshAnimArray_Clear(swigCPtr);
  }

  public void Reset() {
    assimp_swigPINVOKE.aiMeshAnimArray_Reset(swigCPtr);
  }

  public void Reserve(uint size, bool exact) {
    assimp_swigPINVOKE.aiMeshAnimArray_Reserve__SWIG_0(swigCPtr, size, exact);
  }

  public void Reserve(uint size) {
    assimp_swigPINVOKE.aiMeshAnimArray_Reserve__SWIG_1(swigCPtr, size);
  }

  public uint Size() {
    uint ret = assimp_swigPINVOKE.aiMeshAnimArray_Size(swigCPtr);
    return ret;
  }

  public aiMeshAnim Get(uint index) {
    aiMeshAnim ret = new aiMeshAnim(assimp_swigPINVOKE.aiMeshAnimArray_Get(swigCPtr, index), false);
    return ret;
  }

  public bool Set(uint index, aiMeshAnim value) {
    bool ret = assimp_swigPINVOKE.aiMeshAnimArray_Set(swigCPtr, index, aiMeshAnim.getCPtr(value));
    return ret;
  }

}

}