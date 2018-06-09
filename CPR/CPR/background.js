var host_name = "cpr";
var port = null;
var currentpage = "";
var sendMessage = false;

var winTop=-1;
var winLeft=-1;
var winHeight=-1;
var winWidth=-1;
var scroll="S:0,0";

var startkeyword = "";

chrome.browserAction.onClicked.addListener(function(){
	//Get First URL and Scroll Pos
	getCurrentURL();
	//Get First Window Position
	windowInfo();

	console.log('Connecting to native host: ' + host_name);
    port = chrome.runtime.connectNative(host_name);
    //sendNativeMessage(currentpage);
    port.onMessage.addListener(onNativeMessage);
    port.onDisconnect.addListener(onDisconnected);
});

function sendNativeMessage(msg) {
    message = {"text" : msg};
    console.log('Sending message to native app: ' + JSON.stringify(message));
    port.postMessage(message);
    console.log('Sent message to native app: ' + msg);
}

function onNativeMessage(message) {
    console.log('recieved message from native app: ' + JSON.stringify(message));
    if(message['signal'] == "get"){
    	console.log("get");
    	sendMessage = true;
    	if(startkeyword!=""){
    		sendNativeMessage("URLST:"+currentpage+","+startkeyword);
    	}
    	else{
    		sendNativeMessage("URLST:"+currentpage);
    	}
    	sendNativeMessage("WD:"+winLeft+","+winTop+","+(winLeft+winWidth)+","+(winTop+winHeight));
    	if(currentpage.startsWith("chrome://") == true){
    		scroll="S:0,0";
    	}
    	if(scroll!=null){sendNativeMessage(scroll);}
    }
    else if(message['signal'] == "end"){
    	console.log("end");
    	sendMessage = false;
    }
}

function onDisconnected() {
	sendMessage = false;
    console.log(chrome.runtime.lastError);
    console.log('disconnected from native app.');
    port = null;
    alert("App Disconnect!");
}

chrome.tabs.onActivated.addListener(function (activeInfo) {
	chrome.tabs.get(activeInfo.tabId, function (tab) {
		if (chrome.runtime.lastError) {
			console.log(chrome.runtime.lastError.message);
		} 
		else {
			if(currentpage != tab.url){
				currentpage = tab.url;
				console.log(currentpage);
				if(currentpage.startsWith("https://www.google.com.tw/search") == true){
					var title = tab.title;
					console.log(title);
					if(title != null){
						var keyword = title.slice(0, title.length-11).trim();
						startkeyword = keyword;
						console.log(keyword);
						if(sendMessage == true){
							sendNativeMessage("URLK:"+currentpage+","+keyword);
						}
					}
				}
				else{
					startkeyword = "";
					if(sendMessage == true){
						sendNativeMessage("URL:"+currentpage);
					}
				}
				//ScrollTop
				chrome.tabs.sendMessage(tab.id, {action: "changeTab" }, function(response) {});
			}
			if(currentpage.startsWith("chrome://") == true){
				//ScrollTop
				if(sendMessage == true){
					sendNativeMessage("S:0,0");
				}
				console.log("S:0,0");
			}
		}
	});
	console.log("==1");
});

chrome.tabs.onUpdated.addListener(function (tabId, changeInfo ,tab) {
	chrome.tabs.query({active: true, currentWindow: true}, function(tabs) {
		if (chrome.runtime.lastError) {
			console.log(chrome.runtime.lastError.message);
		} 
		else {
			if( tabs.length == 0) return;
			if( tabs[0].url == tab.url ){
				if(tab.url.startsWith("https://www.google.com.tw/search") == false){
					if(currentpage != tab.url){
						currentpage = tab.url;
						console.log(currentpage);
						startkeyword = "";
						if(sendMessage == true){
							sendNativeMessage("URL:"+currentpage);
							//sendNativeMessage("S:0,0");
						}
						//console.log("S:0,0");
						//ScrollTop
						chrome.tabs.sendMessage(tab.id, {action: "changeTab" }, function(response) {});
						if(currentpage.startsWith("chrome://") == true){
							//ScrollTop
							if(sendMessage == true){
								sendNativeMessage("S:0,0");
							}
							console.log("S:0,0");
						}
					}
				}
				else{
					if(tab.title.startsWith("https://www.google.com.tw/search") == false ){
						if(currentpage != tab.url){
							currentpage = tab.url;
							console.log(currentpage);
							var title = tab.title;
							var keyword = title.slice(0, title.length-11).trim();
							startkeyword = keyword;
							console.log(title);
							console.log(keyword);
							if(sendMessage == true){
								sendNativeMessage("URLK:"+currentpage+","+keyword);
								//sendNativeMessage("S:0,0");
							}
							//console.log("S:0,0");
							//ScrollTop
							chrome.tabs.sendMessage(tab.id, {action: "changeTab" }, function(response) {});
						}
					}
				}
				/*
				if(tab.status=='complete'){
					//ScrollTop
					chrome.tabs.sendMessage(tab.id, {action: "changeTab" }, function(response) {});
				}*/
			}
		}
	});

    
});

chrome.tabs.onRemoved.addListener(function (tabId, removeInfo) {
	if (chrome.runtime.lastError) {
		console.log(chrome.runtime.lastError.message);
	} 
	else {
		chrome.tabs.query({
			active: true
		}, function(tabs) {
			if (chrome.runtime.lastError) {
				console.log(chrome.runtime.lastError.message);
			} 
			else {
				if( tabs.length == 0) return;
				var url = tabs[0].url;
				if(currentpage != url){
					currentpage = url;
					console.log(currentpage);
					if(currentpage.startsWith("https://www.google.com.tw/search") == true){
						var title = tabs[0].title;
						console.log(title);
						if(title != null){
							var keyword = title.slice(0, title.length-11).trim();
							startkeyword = keyword;
							console.log(keyword);
							if(sendMessage == true){
								sendNativeMessage("URLK:"+currentpage+","+keyword);
							}
						}
					}
					else{
						startkeyword = "";
						if(sendMessage == true){
							sendNativeMessage("URL:"+currentpage);
						}
					}
					//ScrollTop
					chrome.tabs.sendMessage(tabs[0].id, {action: "changeTab" }, function(response) {});
				}
			}
		});
	}
	console.log("==3");
});

chrome.runtime.onMessage.addListener( function(request, sender, sendResponse) {
	//console.log(sender.tab ?"from a content script:" + sender.tab.url :"from the extension");
	console.log(request.greeting);
	if(sendMessage == true){
		sendNativeMessage(request.greeting);
	}
	if(request.greeting.startsWith("S:") == true){
		scroll = request.greeting;
	}
});

function windowInfo(){
	chrome.windows.getCurrent( function(win) {
		if (chrome.runtime.lastError) {
			console.log(chrome.runtime.lastError.message);
		} 
		else if(win!=null){
			var winChange = false;
			if(winTop != win.top){
				winTop = win.top;
				winChange = true;
			}
			if(winLeft != win.left){
				winLeft = win.left;
				winChange = true;
			}
			if(winHeight != win.height){
				winHeight = win.height;
				winChange = true;
			}
			if(winWidth != win.width){
				winWidth = win.width;
				winChange = true;
			}
			if(winChange){
				console.log("x1:"+winLeft+", y1:"+winTop+", x2:"+(winLeft+winWidth)+", y2:"+(winTop+winHeight));
				if(sendMessage == true){
					sendNativeMessage("WD:"+winLeft+","+winTop+","+(winLeft+winWidth)+","+(winTop+winHeight));
				}
			}
		}
	});
}
var t = setInterval(windowInfo, 100);

function getCurrentURL(){
	if (chrome.runtime.lastError) {
		console.log(chrome.runtime.lastError.message);
	} 
	else {
		chrome.tabs.query({active: true, currentWindow: true}, function(tabs) {
			if (chrome.runtime.lastError) {
				console.log(chrome.runtime.lastError.message);
			} 
			else {
				if( tabs.length == 0) return;
				var url = tabs[0].url;
				if(currentpage != url){
					currentpage = url;
					console.log(currentpage);
					if(currentpage.startsWith("https://www.google.com.tw/search") == true){
						var title = tabs[0].title;
						console.log(title);
						if(title != null){
							var keyword = title.slice(0, title.length-11).trim();
							startkeyword = keyword;
							console.log(keyword);
						}
					}
				}
				//ScrollTop
				chrome.tabs.sendMessage(tabs[0].id, {action: "changeTab" }, function(response) {});
			}
		});
	}
}
