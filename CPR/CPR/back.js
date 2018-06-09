var xClient;
var yClient;
var timeout;
var startScroll = true;

// document.onmousemove = function(e)
// {
// 	var x = e.pageX;
// 	var y = e.pageY;
// 	xClient = e.clientX;
// 	yClient = e.clientY;
// 	//if(x%10==0 || y%10==0){
// 	chrome.runtime.sendMessage({greeting: "P:" + x + "," + y +":"+xClient+", "+yClient }, function(response) {});
// 	//}
// 	//chrome.runtime.sendMessage({greeting: xPage + "," + yPage }, function(response) {});
// };

window.onscroll = function() {myFunction()};

function myFunction() {
	var x = parseInt(window.scrollX);
	var y = parseInt(window.scrollY);
	var cx = parseInt(window.scrollX+xClient);
	var cy = parseInt(window.scrollY+yClient);
	//chrome.runtime.sendMessage({greeting: "P:" + cx + "," + cy }, function(response) {});
	if(startScroll){
		chrome.runtime.sendMessage({greeting: "SCROLLST:"+ x + "," + y }, function(response) {});
		startScroll = false;
	}
	if(!startScroll){
		chrome.runtime.sendMessage({greeting: "D:" + x + "," + y }, function(response) {});
	}
	clearTimeout(timeout);
	timeout = setTimeout(function(){ 
		chrome.runtime.sendMessage({greeting: "SCROLLE:"+ x + "," + y }, function(response) {});
		startScroll = true;
	}, 100);
	//chrome.runtime.sendMessage({greeting: "scrollTop:"+ x + "," + y + "+" + cx + "," + cy}, function(response) {});
}

// var winX;
// var winY;
// function windowInfo(){
// 	if(winX != window.screenX){
// 		winX = window.screenX;
// 	}
// 	if(winY != window.screenY){
// 		winY = window.screenY;
// 	}
// }

chrome.extension.onMessage.addListener( function(request, sender, sendResponse) {
	//sendResponse({farewell: "goodbye"});
	chrome.runtime.sendMessage({greeting: "S:" + parseInt(window.scrollX) + "," + parseInt(window.scrollY) }, function(response) {});
});