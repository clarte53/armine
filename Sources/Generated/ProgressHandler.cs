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

public class ProgressHandler : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal ProgressHandler(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(ProgressHandler obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~ProgressHandler() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          assimp_swigPINVOKE.delete_ProgressHandler(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public virtual bool Update(float percentage) {
    bool ret = assimp_swigPINVOKE.ProgressHandler_Update__SWIG_0(swigCPtr, percentage);
    return ret;
  }

  public virtual bool Update() {
    bool ret = assimp_swigPINVOKE.ProgressHandler_Update__SWIG_1(swigCPtr);
    return ret;
  }

  public virtual void UpdateFileRead(int currentStep, int numberOfSteps) {
    assimp_swigPINVOKE.ProgressHandler_UpdateFileRead(swigCPtr, currentStep, numberOfSteps);
  }

  public virtual void UpdatePostProcess(int currentStep, int numberOfSteps) {
    assimp_swigPINVOKE.ProgressHandler_UpdatePostProcess(swigCPtr, currentStep, numberOfSteps);
  }

  public virtual void UpdateFileWrite(int currentStep, int numberOfSteps) {
    assimp_swigPINVOKE.ProgressHandler_UpdateFileWrite(swigCPtr, currentStep, numberOfSteps);
  }

}

}