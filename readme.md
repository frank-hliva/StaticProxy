# StaticProxy

StaticProxy generator

## License:

https://github.com/frank-hliva/StaticProxy/blob/master/LICENSE.md

## Example:

```fs
open System.IO
open System.Net

let proxy = StaticProxy.fromType<HttpListenerRequest> "Request"
File.WriteAllText(@"proxy.fs", proxy)
```