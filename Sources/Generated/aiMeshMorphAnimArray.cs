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

public class aiMeshMorphAnimArray : global::System.IDisposable, Interface.Array<aiMeshMorphAnim> {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal aiMeshMorphAnimArray(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(aiMeshMorphAnimArray obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~aiMeshMorphAnimArray() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          assimp_swigPINVOKE.delete_aiMeshMorphAnimArray(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public void Clear() {
    assimp_swigPINVOKE.aiMeshMorphAnimArray_Clear(swigCPtr);
  }

  public void Reset() {
    assimp_swigPINVOKE.aiMeshMorphAnimArray_Reset(swigCPtr);
  }

  public void Reserve(uint size, bool exact) {
    assimp_swigPINVOKE.aiMeshMorphAnimArray_Reserve__SWIG_0(swigCPtr, size, exact);
  }

  public void Reserve(uint size) {
    assimp_swigPINVOKE.aiMeshMorphAnimArray_Reserve__SWIG_1(swigCPtr, size);
  }

  public uint Size() {
    uint ret = assimp_swigPINVOKE.aiMeshMorphAnimArray_Size(swigCPtr);
    return ret;
  }

  public aiMeshMorphAnim Get(uint index) {
    aiMeshMorphAnim ret = new aiMeshMorphAnim(assimp_swigPINVOKE.aiMeshMorphAnimArray_Get(swigCPtr, index), false);
    return ret;
  }

  public bool Set(uint index, aiMeshMorphAnim value) {
    bool ret = assimp_swigPINVOKE.aiMeshMorphAnimArray_Set(swigCPtr, index, aiMeshMorphAnim.getCPtr(value));
    return ret;
  }

}

}