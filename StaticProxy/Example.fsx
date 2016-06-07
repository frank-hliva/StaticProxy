#load "Proxies.fs"
#load "StaticProxy.fs"
#load "ProxyGenerator.fs"

open System
open System.IO
open System.Net
open Proxies

let proxy = ProxyGenerator().ProxyFromType<HttpListenerRequest>("Request", false)
File.WriteAllText(@"c:\Projekty\proxy.fs", proxy)