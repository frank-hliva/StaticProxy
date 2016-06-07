# StaticProxy

StaticProxy generator

## License:

https://github.com/frank-hliva/StaticProxy/blob/master/LICENSE.md

## Example:

```fs
open System.IO
open System.Net
open Proxies

let proxy = ProxyGenerator().ProxyFromType<HttpListenerRequest>("Request")
File.WriteAllText(@"c:\Projekty\proxy.fs", proxy)
```