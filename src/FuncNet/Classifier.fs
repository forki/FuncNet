namespace FuncNet

/// Classifier definition
type Classifier<'a, 'b> = 'a -> Future<'b> 

/// Common classifier definitions
[<RequireQualifiedAccessAttribute>]
module Classifier =
    open System

    /// Generic classifier exception
    type ClassifierException(msg) = inherit Exception(msg) 
