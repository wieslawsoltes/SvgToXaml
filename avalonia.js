var h=(b,e,t)=>new Promise((n,i)=>{var o=s=>{try{a(t.next(s))}catch(u){i(u)}},r=s=>{try{a(t.throw(s))}catch(u){i(u)}},a=s=>s.done?n(s.value):Promise.resolve(s.value).then(o,r);a((t=t.apply(b,e)).next())});var m=class{constructor(e,t,n){this.renderLoopEnabled=!1;this.renderLoopRequest=0;if(this.htmlCanvas=t,this.renderFrameCallback=n,e){let i=m.createWebGLContext(t);if(!i){console.error("Failed to create WebGL context");return}let o=globalThis.AvaloniaGL;o.makeContextCurrent(i);let r=o.currentContext.GLctx,a=r.getParameter(r.FRAMEBUFFER_BINDING);this.glInfo={context:i,fboId:a?a.id:0,stencil:r.getParameter(r.STENCIL_BITS),sample:0,depth:r.getParameter(r.DEPTH_BITS)}}}static initGL(e,t,n){let i=m.init(!0,e,t,n);return!i||!i.glInfo?null:i.glInfo}static init(e,t,n,i){let o=t;if(!o)return console.error("No canvas element was provided."),null;m.elements||(m.elements=new Map),m.elements.set(n,t);let r=new m(e,t,i);return o.Canvas=r,r}setEnableRenderLoop(e){this.renderLoopEnabled=e,e?this.requestAnimationFrame():this.renderLoopRequest!==0&&(window.cancelAnimationFrame(this.renderLoopRequest),this.renderLoopRequest=0)}requestAnimationFrame(e){e!==void 0&&this.renderLoopEnabled!==e&&this.setEnableRenderLoop(e),this.renderLoopRequest===0&&(this.renderLoopRequest=window.requestAnimationFrame(()=>{var t,n;this.htmlCanvas.width!==this.newWidth&&(this.htmlCanvas.width=(t=this.newWidth)!=null?t:0),this.htmlCanvas.height!==this.newHeight&&(this.htmlCanvas.height=(n=this.newHeight)!=null?n:0),this.renderFrameCallback(),this.renderLoopRequest=0,this.renderLoopEnabled&&this.requestAnimationFrame()}))}setCanvasSize(e,t){this.renderLoopRequest!==0&&(window.cancelAnimationFrame(this.renderLoopRequest),this.renderLoopRequest=0),this.newWidth=e,this.newHeight=t,this.htmlCanvas.width!==this.newWidth&&(this.htmlCanvas.width=this.newWidth),this.htmlCanvas.height!==this.newHeight&&(this.htmlCanvas.height=this.newHeight),this.requestAnimationFrame()}static setCanvasSize(e,t,n){let i=e;!i||!i.Canvas||i.Canvas.setCanvasSize(t,n)}static requestAnimationFrame(e,t){let n=e;!n||!n.Canvas||n.Canvas.requestAnimationFrame(t)}static createWebGLContext(e){let t={alpha:1,depth:1,stencil:8,antialias:0,premultipliedAlpha:1,preserveDrawingBuffer:0,preferLowPowerToHighPerformance:0,failIfMajorPerformanceCaveat:0,majorVersion:2,minorVersion:0,enableExtensionsByDefault:1,explicitSwapControl:0,renderViaOffscreenBackBuffer:1},n=globalThis.AvaloniaGL,i=n.createContext(e,t);return!i&&t.majorVersion>1&&(console.warn("Falling back to WebGL 1.0"),t.majorVersion=1,t.minorVersion=0,i=n.createContext(e,t)),i}},d=class{static observe(e,t,n){if(!e||!n)return;d.lastMove=Date.now(),n(e.clientWidth,e.clientHeight);let i=o=>{Date.now()-d.lastMove>33&&(n(e.clientWidth,e.clientHeight),d.lastMove=Date.now())};window.addEventListener("resize",i)}static unobserve(e){if(!e||!d.observer)return;let t=d.elements.get(e);t&&(d.elements.delete(e),d.observer.unobserve(t))}static init(){d.observer||(d.elements=new Map,d.observer=new ResizeObserver(e=>{for(let t of e)d.invoke(t.target)}))}static invoke(e){let n=e.SizeWatcher;if(!(!n||!n.callback))return n.callback(e.clientWidth,e.clientHeight)}},p=class{static getDpi(){return window.devicePixelRatio}static start(e){return p.lastDpi=window.devicePixelRatio,p.timerId=window.setInterval(p.update,1e3),p.callback=e,p.lastDpi}static stop(){window.clearInterval(p.timerId)}static update(){if(!p.callback)return;let e=window.devicePixelRatio,t=p.lastDpi;p.lastDpi=e,Math.abs(t-e)>.001&&p.callback(t,e)}};var g=class{static getCaretCoordinates(e,t,n){var v,l;if(!M)throw new Error("textarea-caret-position#getCaretCoordinates should only be called in a browser");let i=(v=n==null?void 0:n.debug)!=null?v:!1;if(i){let f=document.querySelector("#input-textarea-caret-position-mirror-div");f&&((l=f.parentNode)==null||l.removeChild(f))}let o=document.createElement("div");o.id="input-textarea-caret-position-mirror-div",document.body.appendChild(o);let r=o.style,a=window.getComputedStyle?window.getComputedStyle(e):e.currentStyle,s=e.nodeName==="INPUT";r.whiteSpace="pre-wrap",s||(r.wordWrap="break-word"),r.position="absolute",i||(r.visibility="hidden"),S.forEach(f=>{if(s&&f==="lineHeight")if(a.boxSizing==="border-box"){let x=parseInt(a.height),k=parseInt(a.paddingTop)+parseInt(a.paddingBottom)+parseInt(a.borderTopWidth)+parseInt(a.borderBottomWidth),T=k+parseInt(a.lineHeight);x>T?r.lineHeight=`${x-k}px`:x===T?r.lineHeight=a.lineHeight:r.lineHeight="0"}else r.lineHeight=a.height;else r[f]=a[f]}),I?e.scrollHeight>parseInt(a.height)&&(r.overflowY="scroll"):r.overflow="hidden",o.textContent=e.value.substring(0,t),s&&(o.textContent=o.textContent.replace(/\s/g,"\xA0"));let u=document.createElement("span");u.textContent=e.value.substring(t)||".",o.appendChild(u);let c={top:u.offsetTop+parseInt(a.borderTopWidth),left:u.offsetLeft+parseInt(a.borderLeftWidth),height:parseInt(a.lineHeight)};return i?u.style.backgroundColor="#aaa":document.body.removeChild(o),c}},S=["direction","boxSizing","width","height","overflowX","overflowY","borderTopWidth","borderRightWidth","borderBottomWidth","borderLeftWidth","borderStyle","paddingTop","paddingRight","paddingBottom","paddingLeft","fontStyle","fontVariant","fontWeight","fontStretch","fontSize","fontSizeAdjust","lineHeight","fontFamily","textAlign","textTransform","textIndent","textDecoration","letterSpacing","wordSpacing","tabSize","MozTabSize"],M=typeof window!="undefined",I=M&&window.mozInnerScreenX!=null;var y=class{static subscribeKeyEvents(e,t,n){let i=r=>{t(r.code,r.key,this.getModifiers(r))&&r.preventDefault()};e.addEventListener("keydown",i);let o=r=>{n(r.code,r.key,this.getModifiers(r))&&r.preventDefault()};return e.addEventListener("keyup",o),()=>{e.removeEventListener("keydown",i),e.removeEventListener("keyup",o)}}static subscribeTextEvents(e,t,n,i,o){let r=c=>{let v=c;t(v.type,v.data)&&c.preventDefault()};e.addEventListener("input",r);let a=c=>{n(c)&&c.preventDefault()};e.addEventListener("compositionstart",a);let s=c=>{i(c)&&c.preventDefault()};e.addEventListener("compositionupdate",s);let u=c=>{o(c)&&c.preventDefault()};return e.addEventListener("compositionend",u),()=>{e.removeEventListener("input",r),e.removeEventListener("compositionstart",a),e.removeEventListener("compositionupdate",s),e.removeEventListener("compositionend",u)}}static subscribePointerEvents(e,t,n,i,o,r){let a=l=>{t(l),l.preventDefault()},s=l=>{n(l),l.preventDefault()},u=l=>{i(l),l.preventDefault()},c=l=>{o(l),l.preventDefault()},v=l=>{r(l),l.preventDefault()};return e.addEventListener("pointermove",a),e.addEventListener("pointerdown",s),e.addEventListener("pointerup",u),e.addEventListener("wheel",v),e.addEventListener("pointercancel",c),()=>{e.removeEventListener("pointerover",a),e.removeEventListener("pointerdown",s),e.removeEventListener("pointerup",u),e.removeEventListener("pointercancel",c),e.removeEventListener("wheel",v)}}static subscribeInputEvents(e,t){let n=i=>{t(i.value)&&i.preventDefault()};return e.addEventListener("input",n),()=>{e.removeEventListener("input",n)}}static getCoalescedEvents(e){return e.getCoalescedEvents()}static clearInput(e){e.value=""}static focusElement(e){e.focus()}static setCursor(e,t){t==="pointer"?e.style.removeProperty("cursor"):e.style.cursor=t}static setBounds(e,t,n,i,o,r){e.style.left=t.toFixed(0)+"px",e.style.top=n.toFixed(0)+"px";let{left:a,top:s}=g.getCaretCoordinates(e,r);e.style.left=(t-a).toFixed(0)+"px",e.style.top=(n-s).toFixed(0)+"px"}static hide(e){e.style.display="none"}static show(e){e.style.display="block"}static setSurroundingText(e,t,n,i){!e||(e.value=t,e.setSelectionRange(n,i),e.style.width="20px",e.style.width=`${e.scrollWidth}px`)}static getModifiers(e){let t=0;return e.ctrlKey&&(t|=2),e.altKey&&(t|=1),e.shiftKey&&(t|=4),e.metaKey&&(t|=8),t}};var w=class{static addClass(e,t){e.classList.add(t)}static createAvaloniaHost(e){let t=Math.random().toString(36).replace(/[^a-z]+/g,"").substr(2,10);e.classList.add("avalonia-container"),e.tabIndex=0,e.oncontextmenu=function(){return!1},e.style.overflow="hidden",e.style.touchAction="none";let n=document.createElement("canvas");n.id=`canvas${t}`,n.classList.add("avalonia-canvas"),n.style.backgroundColor="#ccc",n.style.width="100%",n.style.position="absolute";let i=document.createElement("div");i.id=`nativeHost${t}`,i.classList.add("avalonia-native-host"),i.style.left="0px",i.style.top="0px",i.style.width="100%",i.style.height="100%",i.style.position="absolute";let o=document.createElement("input");return o.id=`inputElement${t}`,o.classList.add("avalonia-input-element"),o.autocapitalize="none",o.type="text",o.spellcheck=!1,o.style.padding="0",o.style.margin="0",o.style.position="absolute",o.style.overflow="hidden",o.style.borderStyle="hidden",o.style.outline="none",o.style.background="transparent",o.style.color="transparent",o.style.display="none",o.style.height="20px",o.style.zIndex="-1",o.onpaste=function(){return!1},o.oncopy=function(){return!1},o.oncut=function(){return!1},e.prepend(o),e.prepend(i),e.prepend(n),{host:e,canvas:n,nativeHost:i,inputElement:o}}};var E=class{static canShowOpenFilePicker(){return typeof globalThis.showOpenFilePicker!="undefined"}static canShowSaveFilePicker(){return typeof globalThis.showSaveFilePicker!="undefined"}static canShowDirectoryPicker(){return typeof globalThis.showDirectoryPicker!="undefined"}static isMobile(){var o;let e=(o=globalThis.navigator)==null?void 0:o.userAgentData;if(e)return e.mobile;let t=navigator.userAgent,n=/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i,i=/1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw(n|u)|c55\/|capi|ccwa|cdm|cell|chtm|cldc|cmd|co(mp|nd)|craw|da(it|ll|ng)|dbte|dcs|devi|dica|dmob|do(c|p)o|ds(12|d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(|_)|g1 u|g560|gene|gf5|gmo|go(\.w|od)|gr(ad|un)|haie|hcit|hd(m|p|t)|hei|hi(pt|ta)|hp( i|ip)|hsc|ht(c(| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i(20|go|ma)|i230|iac( ||\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|[a-w])|libw|lynx|m1w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|mcr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|([1-8]|c))|phil|pire|pl(ay|uc)|pn2|po(ck|rt|se)|prox|psio|ptg|qaa|qc(07|12|21|32|60|[2-7]|i)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h|oo|p)|sdk\/|se(c(|0|1)|47|mc|nd|ri)|sgh|shar|sie(|m)|sk0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h|v|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl|tdg|tel(i|m)|tim|tmo|to(pl|sh)|ts(70|m|m3|m5)|tx9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas|your|zeto|zte/i;return n.test(t)||i.test(t.substr(0,4))}};var L=class{static seek(e,t){return h(this,null,function*(){return yield e.seek(t)})}static truncate(e,t){return h(this,null,function*(){return yield e.truncate(t)})}static close(e){return h(this,null,function*(){return yield e.close()})}static write(e,t){return h(this,null,function*(){let n=new Uint8Array(t.byteLength);t.copyTo(n);let i={type:"write",data:n};return yield e.write(i)})}static byteLength(e){return e.size}static sliceArrayBuffer(e,t,n){return h(this,null,function*(){let i=yield e.slice(t,t+n).arrayBuffer();return new Uint8Array(i)})}static toMemoryView(e){return e}};var H=class{},C=class{static createDefaultChild(e){return document.createElement("div")}static createAttachment(){return new H}static initializeWithChildHandle(e,t){e._child=t,e._child.style.position="absolute"}static attachTo(e,t){e._host&&e._child&&e._host.removeChild(e._child),e._host=t,e._host&&e._child&&e._host.appendChild(e._child)}static showInBounds(e,t,n,i,o){e._child&&(e._child.style.top=`${n}px`,e._child.style.left=`${t}px`,e._child.style.width=`${i}px`,e._child.style.height=`${o}px`,e._child.style.display="block")}static hideWithSize(e,t,n){e._child&&(e._child.style.width=`${t}px`,e._child.style.height=`${n}px`,e._child.style.display="none")}static releaseChild(e){e._child&&(e._child=void 0)}};function O(b){return h(this,null,function*(){b.setModuleImports("avalonia",{Caniuse:E,Canvas:m,InputHelper:y,SizeWatcher:d,DpiWatcher:p,AvaloniaDOM:w,StreamHelper:L,NativeControlHost:C})})}export{w as AvaloniaDOM,E as Caniuse,m as Canvas,p as DpiWatcher,y as InputHelper,C as NativeControlHost,d as SizeWatcher,L as StreamHelper,O as registerAvaloniaModule};
//# sourceMappingURL=avalonia.js.map
