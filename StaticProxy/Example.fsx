#load "StaticProxy.fs"

open System.IO
open System.Net

let proxy = StaticProxy.fromType<HttpListenerRequest> "Request"
File.WriteAllText(@"proxy.fs", proxy)