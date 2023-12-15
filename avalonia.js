var v=class{constructor(e,t,n){this.renderLoopEnabled=!1;this.renderLoopRequest=0;if(this.htmlCanvas=t,this.renderFrameCallback=n,e){let i=v.createWebGLContext(t);if(!i){console.error("Failed to create WebGL context");return}let o=globalThis.AvaloniaGL;o.makeContextCurrent(i);let a=o.currentContext.GLctx,r=a.getParameter(a.FRAMEBUFFER_BINDING);this.glInfo={context:i,fboId:r?r.id:0,stencil:a.getParameter(a.STENCIL_BITS),sample:0,depth:a.getParameter(a.DEPTH_BITS)}}}static initGL(e,t,n){let i=v.init(!0,e,t,n);return!i||!i.glInfo?null:i.glInfo}static init(e,t,n,i){let o=t;if(!o)return console.error("No canvas element was provided."),null;v.elements||(v.elements=new Map),v.elements.set(n,t);let a=new v(e,t,i);return o.Canvas=a,a}setEnableRenderLoop(e){this.renderLoopEnabled=e,e?this.requestAnimationFrame():this.renderLoopRequest!==0&&(window.cancelAnimationFrame(this.renderLoopRequest),this.renderLoopRequest=0)}requestAnimationFrame(e){e!==void 0&&this.renderLoopEnabled!==e&&this.setEnableRenderLoop(e),this.renderLoopRequest===0&&(this.renderLoopRequest=window.requestAnimationFrame(()=>{var t,n;this.htmlCanvas.width!==this.newWidth&&(this.htmlCanvas.width=(t=this.newWidth)!=null?t:0),this.htmlCanvas.height!==this.newHeight&&(this.htmlCanvas.height=(n=this.newHeight)!=null?n:0),this.renderFrameCallback(),this.renderLoopRequest=0,this.renderLoopEnabled&&this.requestAnimationFrame()}))}setCanvasSize(e,t){this.renderLoopRequest!==0&&(window.cancelAnimationFrame(this.renderLoopRequest),this.renderLoopRequest=0),this.newWidth=e,this.newHeight=t,this.htmlCanvas.width!==this.newWidth&&(this.htmlCanvas.width=this.newWidth),this.htmlCanvas.height!==this.newHeight&&(this.htmlCanvas.height=this.newHeight),this.requestAnimationFrame()}static setCanvasSize(e,t,n){let i=e;!i||!i.Canvas||i.Canvas.setCanvasSize(t,n)}static requestAnimationFrame(e,t){let n=e;!n||!n.Canvas||n.Canvas.requestAnimationFrame(t)}static createWebGLContext(e){let t={alpha:1,depth:1,stencil:8,antialias:0,premultipliedAlpha:1,preserveDrawingBuffer:0,preferLowPowerToHighPerformance:0,failIfMajorPerformanceCaveat:0,majorVersion:2,minorVersion:0,enableExtensionsByDefault:1,explicitSwapControl:0,renderViaOffscreenBackBuffer:1},n=globalThis.AvaloniaGL,i=n.createContext(e,t);return!i&&t.majorVersion>1&&(console.warn("Falling back to WebGL 1.0"),t.majorVersion=1,t.minorVersion=0,i=n.createContext(e,t)),i}},c=class{static observe(e,t,n){if(!e||!n)return;c.lastMove=Date.now(),n(e.clientWidth,e.clientHeight);let i=o=>{Date.now()-c.lastMove>33&&(n(e.clientWidth,e.clientHeight),c.lastMove=Date.now())};window.addEventListener("resize",i)}static unobserve(e){if(!e||!c.observer)return;let t=c.elements.get(e);t&&(c.elements.delete(e),c.observer.unobserve(t))}static init(){c.observer||(c.elements=new Map,c.observer=new ResizeObserver(e=>{for(let t of e)c.invoke(t.target)}))}static invoke(e){let n=e.SizeWatcher;if(!(!n||!n.callback))return n.callback(e.clientWidth,e.clientHeight)}},d=class{static getDpi(){return window.devicePixelRatio}static start(e){return d.lastDpi=window.devicePixelRatio,d.timerId=window.setInterval(d.update,1e3),d.callback=e,d.lastDpi}static stop(){window.clearInterval(d.timerId)}static update(){if(!d.callback)return;let e=window.devicePixelRatio,t=d.lastDpi;d.lastDpi=e,Math.abs(t-e)>.001&&d.callback(t,e)}};var g=class{static getCaretCoordinates(e,t,n){var h,s;if(!M)throw new Error("textarea-caret-position#getCaretCoordinates should only be called in a browser");let i=(h=n==null?void 0:n.debug)!=null?h:!1;if(i){let m=document.querySelector("#input-textarea-caret-position-mirror-div");m&&((s=m.parentNode)==null||s.removeChild(m))}let o=document.createElement("div");o.id="input-textarea-caret-position-mirror-div",document.body.appendChild(o);let a=o.style,r=window.getComputedStyle?window.getComputedStyle(e):e.currentStyle,u=e.nodeName==="INPUT";a.whiteSpace="pre-wrap",u||(a.wordWrap="break-word"),a.position="absolute",i||(a.visibility="hidden"),F.forEach(m=>{if(u&&m==="lineHeight")if(r.boxSizing==="border-box"){let y=parseInt(r.height),k=parseInt(r.paddingTop)+parseInt(r.paddingBottom)+parseInt(r.borderTopWidth)+parseInt(r.borderBottomWidth),S=k+parseInt(r.lineHeight);y>S?a.lineHeight=`${y-k}px`:y===S?a.lineHeight=r.lineHeight:a.lineHeight="0"}else a.lineHeight=r.height;else a[m]=r[m]}),A?e.scrollHeight>parseInt(r.height)&&(a.overflowY="scroll"):a.overflow="hidden",o.textContent=e.value.substring(0,t),u&&(o.textContent=o.textContent.replace(/\s/g,"\xA0"));let p=document.createElement("span");p.textContent=e.value.substring(t)||".",o.appendChild(p);let l={top:p.offsetTop+parseInt(r.borderTopWidth),left:p.offsetLeft+parseInt(r.borderLeftWidth),height:parseInt(r.lineHeight)};return i?p.style.backgroundColor="#aaa":document.body.removeChild(o),l}},F=["direction","boxSizing","width","height","overflowX","overflowY","borderTopWidth","borderRightWidth","borderBottomWidth","borderLeftWidth","borderStyle","paddingTop","paddingRight","paddingBottom","paddingLeft","fontStyle","fontVariant","fontWeight","fontStretch","fontSize","fontSizeAdjust","lineHeight","fontFamily","textAlign","textTransform","textIndent","textDecoration","letterSpacing","wordSpacing","tabSize","MozTabSize"],M=typeof window!="undefined",A=M&&window.mozInnerScreenX!=null;var f=class{static initializeBackgroundHandlers(){this.clipboardState===0&&(globalThis.addEventListener("paste",e=>{this.clipboardState===2&&this.resolveClipboard(e.clipboardData.getData("text"))}),this.clipboardState=1)}static async readClipboardText(){if(globalThis.navigator.clipboard.readText)return await globalThis.navigator.clipboard.readText();try{return await new Promise((e,t)=>{this.clipboardState=2,this.resolveClipboard=e,this.rejectClipboard=t})}finally{this.clipboardState=1,this.resolveClipboard=null,this.rejectClipboard=null}}static subscribeKeyEvents(e,t,n){let i=a=>{t(a.code,a.key,this.getModifiers(a))&&this.clipboardState!==2&&a.preventDefault()};e.addEventListener("keydown",i);let o=a=>{n(a.code,a.key,this.getModifiers(a))&&a.preventDefault(),this.rejectClipboard&&this.rejectClipboard()};return e.addEventListener("keyup",o),()=>{e.removeEventListener("keydown",i),e.removeEventListener("keyup",o)}}static subscribeTextEvents(e,t,n,i,o){let a=l=>{n(l)&&l.preventDefault()};e.addEventListener("compositionstart",a);let r=l=>{let h=l.getTargetRanges(),s=-1,m=-1;h.length>0&&(s=h[0].startOffset,m=h[0].endOffset),l.inputType==="insertCompositionText"&&(s=2,m=s+2),t(l,s,m)&&l.preventDefault()};e.addEventListener("beforeinput",r);let u=l=>{i(l)&&l.preventDefault()};e.addEventListener("compositionupdate",u);let p=l=>{o(l)&&l.preventDefault()};return e.addEventListener("compositionend",p),()=>{e.removeEventListener("compositionstart",a),e.removeEventListener("compositionupdate",u),e.removeEventListener("compositionend",p)}}static subscribePointerEvents(e,t,n,i,o,a){let r=s=>{t(s),s.preventDefault()},u=s=>{n(s),s.preventDefault()},p=s=>{i(s),s.preventDefault()},l=s=>{o(s),s.preventDefault()},h=s=>{a(s),s.preventDefault()};return e.addEventListener("pointermove",r),e.addEventListener("pointerdown",u),e.addEventListener("pointerup",p),e.addEventListener("wheel",h),e.addEventListener("pointercancel",l),()=>{e.removeEventListener("pointerover",r),e.removeEventListener("pointerdown",u),e.removeEventListener("pointerup",p),e.removeEventListener("pointercancel",l),e.removeEventListener("wheel",h)}}static subscribeInputEvents(e,t){let n=i=>{t(i.value)&&i.preventDefault()};return e.addEventListener("input",n),()=>{e.removeEventListener("input",n)}}static subscribeDropEvents(e,t){let n=i=>{t(i)&&i.preventDefault()};return e.addEventListener("dragover",n),e.addEventListener("dragenter",n),e.addEventListener("dragleave",n),e.addEventListener("drop",n),()=>{e.removeEventListener("dragover",n),e.removeEventListener("dragenter",n),e.removeEventListener("dragleave",n),e.removeEventListener("drop",n)}}static getCoalescedEvents(e){return e.getCoalescedEvents()}static clearInput(e){e.value=""}static focusElement(e){e.focus()}static setCursor(e,t){t==="default"?e.style.removeProperty("cursor"):e.style.cursor=t}static setBounds(e,t,n,i,o,a){e.style.left=t.toFixed(0)+"px",e.style.top=n.toFixed(0)+"px";let{left:r,top:u}=g.getCaretCoordinates(e,a);e.style.left=(t-r).toFixed(0)+"px",e.style.top=(n-u).toFixed(0)+"px"}static hide(e){e.style.display="none"}static show(e){e.style.display="block"}static setSurroundingText(e,t,n,i){!e||(e.value=t,e.setSelectionRange(n,i),e.style.width="20px",e.style.width=`${e.scrollWidth}px`)}static getModifiers(e){let t=0;return e.ctrlKey&&(t|=2),e.altKey&&(t|=1),e.shiftKey&&(t|=4),e.metaKey&&(t|=8),t}};f.clipboardState=0;var w=class{static addClass(e,t){e.classList.add(t)}static observeDarkMode(e){if(globalThis.matchMedia===void 0)return!1;let t=globalThis.matchMedia("(prefers-color-scheme: dark)"),n=globalThis.matchMedia("(prefers-contrast: more)");return t.addEventListener("change",i=>{e(i.matches,n.matches)}),n.addEventListener("change",i=>{e(t.matches,i.matches)}),{isDarkMode:t.matches,isHighContrast:n.matches}}static createAvaloniaHost(e){let t=Math.random().toString(36).replace(/[^a-z]+/g,"").substr(2,10);e.classList.add("avalonia-container"),e.tabIndex=0,e.oncontextmenu=function(){return!1},e.style.overflow="hidden",e.style.touchAction="none";let n=document.createElement("canvas");n.id=`canvas${t}`,n.classList.add("avalonia-canvas"),n.style.width="100%",n.style.position="absolute";let i=document.createElement("div");i.id=`nativeHost${t}`,i.classList.add("avalonia-native-host"),i.style.left="0px",i.style.top="0px",i.style.width="100%",i.style.height="100%",i.style.position="absolute";let o=document.createElement("input");return o.id=`inputElement${t}`,o.classList.add("avalonia-input-element"),o.autocapitalize="none",o.type="text",o.spellcheck=!1,o.style.padding="0",o.style.margin="0",o.style.position="absolute",o.style.overflow="hidden",o.style.borderStyle="hidden",o.style.outline="none",o.style.background="transparent",o.style.color="transparent",o.style.display="none",o.style.height="20px",o.style.zIndex="-1",o.onpaste=function(){return!1},o.oncopy=function(){return!1},o.oncut=function(){return!1},e.prepend(o),e.prepend(i),e.prepend(n),{host:e,canvas:n,nativeHost:i,inputElement:o}}static isFullscreen(){return document.fullscreenElement!=null}static async setFullscreen(e){e?await document.documentElement.requestFullscreen():await document.exitFullscreen()}static getSafeAreaPadding(){let e=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sat")),t=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sab")),n=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sal")),i=parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sar"));return[n,e,t,i]}};var E=class{static hasNativeFilePicker(){return"showSaveFilePicker"in globalThis}static isMobile(){var o;let e=(o=globalThis.navigator)==null?void 0:o.userAgentData;if(e)return e.mobile;let t=navigator.userAgent,n=/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i,i=/1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw(n|u)|c55\/|capi|ccwa|cdm|cell|chtm|cldc|cmd|co(mp|nd)|craw|da(it|ll|ng)|dbte|dcs|devi|dica|dmob|do(c|p)o|ds(12|d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(|_)|g1 u|g560|gene|gf5|gmo|go(\.w|od)|gr(ad|un)|haie|hcit|hd(m|p|t)|hei|hi(pt|ta)|hp( i|ip)|hsc|ht(c(| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i(20|go|ma)|i230|iac( ||\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|[a-w])|libw|lynx|m1w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|mcr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|([1-8]|c))|phil|pire|pl(ay|uc)|pn2|po(ck|rt|se)|prox|psio|ptg|qaa|qc(07|12|21|32|60|[2-7]|i)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h|oo|p)|sdk\/|se(c(|0|1)|47|mc|nd|ri)|sgh|shar|sie(|m)|sk0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h|v|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl|tdg|tel(i|m)|tim|tmo|to(pl|sh)|ts(70|m|m3|m5)|tx9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas|your|zeto|zte/i;return n.test(t)||i.test(t.substr(0,4))}};var L=class{static async seek(e,t){return await e.seek(t)}static async truncate(e,t){return await e.truncate(t)}static async close(e){return await e.close()}static async write(e,t){let n=new Uint8Array(t.byteLength);return t.copyTo(n),await e.write(n)}static byteLength(e){return e.size}static async sliceArrayBuffer(e,t,n){let i=await e.slice(t,t+n).arrayBuffer();return new Uint8Array(i)}static toMemoryView(e){return e}};var C=class{},x=class{static createDefaultChild(e){return document.createElement("div")}static createAttachment(){return new C}static initializeWithChildHandle(e,t){e._child=t,e._child.style.position="absolute"}static attachTo(e,t){e._host&&e._child&&e._host.removeChild(e._child),e._host=t,e._host&&e._child&&e._host.appendChild(e._child)}static showInBounds(e,t,n,i,o){e._child&&(e._child.style.top=`${n}px`,e._child.style.left=`${t}px`,e._child.style.width=`${i}px`,e._child.style.height=`${o}px`,e._child.style.display="block")}static hideWithSize(e,t,n){e._child&&(e._child.style.width=`${t}px`,e._child.style.height=`${n}px`,e._child.style.display="none")}static releaseChild(e){e._child&&(e._child=void 0)}};var H=class{static addBackHandler(e){history.pushState(null,"",window.location.href),window.onpopstate=()=>{e()?history.forward():history.back()}}};var T=class{static itemsArrayAt(e,t){let n=e[t];if(!n)return[];let i=[];for(let o=0;o<n.length;o++)i[o]=n[o];return i}static itemAt(e,t){return e[t]}static callMethod(e,t){let n=Array.prototype.slice.call(arguments,2);return e[t].apply(e,n)}};async function Q(b,e){"serviceWorker"in navigator&&await globalThis.navigator.serviceWorker.register(b,e?{scope:e}:void 0)}export{w as AvaloniaDOM,E as Caniuse,v as Canvas,d as DpiWatcher,T as GeneralHelpers,f as InputHelper,x as NativeControlHost,H as NavigationHelper,c as SizeWatcher,L as StreamHelper,Q as registerServiceWorker};
//# sourceMappingURL=avalonia.js.map
