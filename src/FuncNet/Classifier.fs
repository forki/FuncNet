namespace FuncNet

/// Common classifier definitions
[<RequireQualifiedAccessAttribute>]
module Classifier =
    open System

    /// Generic classifier exception
    type ClassifierException(msg) = inherit Exception(msg) 
