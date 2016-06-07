namespace Proxies

type ProxyGenerator() =
    member m.ProxyFromType<'t>(settings : ProxySettings) = StaticProxy.fromType<'t> settings
    member m.ProxyFromType<'t>(proxyClassName, ?objectName, ?fullNames : bool) =
        let proxySettings = {
            ProxyClassName = proxyClassName
            ObjectName = defaultArg objectName ""
            FullNames = defaultArg fullNames false
        }
        m.ProxyFromType<'t>(proxySettings)
    member m.ProxyFromType<'t>(proxyClassName, fullNames : bool) =
        m.ProxyFromType<'t>(proxyClassName, "", fullNames)