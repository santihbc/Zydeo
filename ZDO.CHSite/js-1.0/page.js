﻿/// <reference path="/lib/jquery-2.1.4.min.js" />
/// <reference path="/lib/history.min.js" />

function startsWith(str, prefix) {
  if (str.length < prefix.length)
    return false;
  for (var i = prefix.length - 1; (i >= 0) && (str[i] === prefix[i]) ; --i)
    continue;
  return i < 0;
}

var zdPage = (function () {
  "use strict";

  var reqId = 0; // Current page load request ID. If page has moved on, earlier requests ignored when they complete.
  var location = null; // Full location, as seen in navbar
  var path = null; // Path after domain name
  var lang = null; // Language (first section of path)
  var rel = null; // Relative path (path without language ID at start)

  // Page init scripts for each page (identified by relPath).
  var initScripts = {};
  // Global init scripts invoked on documentReady.
  var globalInitScripts = [];

  // Close function of currently active modal popup, or null.
  var activeModalCloser = null;

  // Incremented for subsequent alerts, so we can correctly animate new one shown before old one has expired.
  var alertId = 0;

  var alertTemplate =
    '<div class="alertBar" id="alertBarId">' +
    '  <div class="alert" id="alertId">' +
    '    <div class="alertMessage"><span class="alertTitle" /><span class="alertBody" /></div>' +
    '    <div class="alertClose"><img src="/static/close.svg" alt="" /></div>' +
    '  </div>' +
    '</div>';

  // Parse full path, language, and relative path from URL
  function parseLocation() {
    location = window.history.location || window.location;
    var rePath = /https?:\/\/[^\/]+(.*)/i;
    var match = rePath.exec(location);
    path = match[1];
    if (startsWith(path, "/en/") || path == "/en") {
      lang = "en";
      rel = path == "/en" ? "" : path.substring(4);
    }
    else if (startsWith(path, "/hu/") || path == "/hu") {
      lang = "hu";
      rel = path == "/hu" ? "" : path.substring(4);
    }
    else {
      lang = "hu";
      rel = path;
    }
  }

  // Page just loaded: time to get dynamic part asynchronously, wherever we just landed
  $(document).ready(function () {
    // Make sense of location
    parseLocation();
    // Update menu to show where I am (will soon end up being)
    updateMenuState();
    // Global script initializers
    for (var i = 0; i != globalInitScripts.length; ++i) globalInitScripts[i]();
    // Request dynamic page - async
    ++reqId;
    var id = reqId;
    var req = $.ajax({
      url: "/Handler.ashx",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8",
      data: { action: "dynpage", lang: lang, rel: rel }
    });
    req.done(function (data) {
      dynReady(data, id);
    });
    req.fail(function (jqXHR, textStatus, error) {
      applyFailHtml();
    });
    // Generic click-away handler to close active popup
    $('html').click(function () {
      if (activeModalCloser != null) {
        activeModalCloser();
        activeModalCloser = null;
      }
    });
    // Adapt font size to window width
    $(window).resize(onResize);
    onResize();
  });

  // Measure m width against viewport; adapt font size
  function onResize() {
    var ww = window.innerWidth;
    var w10em = $("#emMeasure")[0].clientWidth;

    var frac = ww / w10em;
    var ptStyle;
    if (frac < 5.4) ptStyle = "pt7";
    else if (frac < 6.0) ptStyle = "pt8";
    else if (frac < 6.6) ptStyle = "pt9";
    else if (frac < 7.2) ptStyle = "pt10";
    else if (frac < 7.8) ptStyle = "pt11";
    else if (frac < 8.4) ptStyle = "pt12";
    else if (frac < 9) ptStyle = "pt13";
    else if (frac > 12) ptStyle = "pt16";
    else ptStyle = "pt14";
    var theBody = $("#theBody");
    if (!theBody.hasClass(ptStyle)) {
      theBody.removeClass("pt7");
      theBody.removeClass("pt8");
      theBody.removeClass("pt9");
      theBody.removeClass("pt10");
      theBody.removeClass("pt11");
      theBody.removeClass("pt12");
      theBody.removeClass("pt13");
      theBody.removeClass("pt14");
      theBody.removeClass("pt16");
      theBody.addClass(ptStyle);
    }
  }

  // Navigate within single-page app (invoked from link click handler)
  function dynNavigate() {
    // Make sense of location
    parseLocation();
    // Clear whatever's currently shown
    //$("#dynPage").html("");
    $("#dynPage").addClass("fading");
    // Update menu to show where I am (will soon end up being)
    updateMenuState();
    // Request dynamic page - async
    ++reqId;
    var id = reqId;
    var req = $.ajax({
      url: "/Handler.ashx",
      type: "POST",
      contentType: "application/x-www-form-urlencoded; charset=UTF-8",
      data: { action: "dynpage", lang: lang, rel: rel }
    });
    req.done(function (data) {
      navReady(data, id);
    });
    req.fail(function (jqXHR, textStatus, error) {
      applyFailHtml();
    });
  }

  // Show error content in dynamic area
  function applyFailHtml() {
    $("#dynPage").html("Ouch.");
    // TO-DO: fail title; keywords; description
  }

  // Apply dynamic content: HTML body, title, description, keywords
  function applyDynContent(html, title, description, keywords) {
    $(document).attr("title", title);
    $("meta[name = 'keywords']").attr("content", keywords);
    $("meta[name = 'description']").attr("content", description);
    $("#dynPage").html(html);
    $("#dynPage").removeClass("fading");
    // Run this page's script initializer, if any
    for (var key in initScripts) {
      if (startsWith(rel, key)) initScripts[key]();
      // Hack: call search initializer for ""
      if (rel == "" && key == "search") initScripts[key]();
    }
    // Scroll to top
    $(window).scrollTop(0);
  }

  function navReady(data, id) {
    // An obsolete request completing too late?
    if (id != reqId) return;

    // Show dynamic content, title etc.
    applyDynContent(data.html, data.title, data.description, data.keywords);
  }

  // Dynamic data received after initial page load (not within single-page navigation)
  function dynReady(data, id) {
    // An obsolete request completing too late?
    if (id != reqId) return;

    // Show dynamic content, title etc.
    applyDynContent(data.html, data.title, data.description, data.keywords);

    // Set up single-page navigation
    $(document).on('click', 'a.ajax', function () {
      // Navigation closes any active modal popup
      if (activeModalCloser != null) {
        activeModalCloser();
        activeModalCloser = null;
      }
      // Trick: If we're on search page but menu is shown, link just changes display; no navigation
      if ((rel == "" || startsWith(rel, "search")) && $(this).attr("id") == "topMenuSearch") {
        $("#hdrSearch").addClass("on");
        $("#hdrMenu").removeClass("on");
        $("#subHeader").removeClass("visible");
        return false;
      }
      // Navigate
      history.pushState(null, null, this.href);
      dynNavigate();
      return false;
    });
    $(window).on('popstate', function (e) {
      dynNavigate();
    });

    // *NOW* that we're all done, show page.
    $("#thePage").addClass("visible");
    // Events - toggle from lookup input to menu
    $("#toMenu").click(function () {
      $("#hdrSearch").removeClass("on");
      $("#hdrMenu").addClass("on");
      //$("#subHeader").addClass("visible");
    });
  }

  // Updates top navigation menu to reflect where we are
  function updateMenuState() {
    $(".topMenu").removeClass("on");
    $(".subMenu").removeClass("visible");
    if (rel == "" || startsWith(rel, "search")) {
      $("#hdrMenu").removeClass("on");
      $("#subHeader").removeClass("visible");
      $("#dynPage").addClass("nosubmenu");
      $("#headermask").addClass("nosubmenu");
      $("#hdrSearch").addClass("on");
    }
    else {
      $("#hdrSearch").removeClass("on");
      $("#hdrMenu").addClass("on");
      $("#subHeader").addClass("visible");
      $("#dynPage").removeClass("nosubmenu");
      $("#headermask").removeClass("nosubmenu");
      if (startsWith(rel, "edit")) {
        $("#topMenuEdit").addClass("on");
        $("#subMenuEdit").addClass("visible");
      }
      else if (startsWith(rel, "read")) {
        $("#topMenuRead").addClass("on");
        $("#subMenuRead").addClass("visible");
      }
      else if (startsWith(rel, "download")) {
        $("#topMenuDownload").addClass("on");
        $("#subMenuDownload").addClass("visible");
      }
    }
    $(".subMenu span").removeClass("on");
    if (startsWith(rel, "edit/new")) $("#smEditNew").addClass("on");
    else if (startsWith(rel, "edit/history")) $("#smEditHistory").addClass("on");
    else if (startsWith(rel, "edit/existing")) $("#smEditExisting").addClass("on");
    else if (startsWith(rel, "read/about")) $("#smReadAbout").addClass("on");
    else if (startsWith(rel, "read/articles")) $("#smReadArticles").addClass("on");
    else if (startsWith(rel, "read/etc")) $("#smReadEtc").addClass("on");
    // Language selector
    $("#langSelHu").attr("href", "/hu/" + rel);
    $("#langSelEn").attr("href", "/en/" + rel);
    $(".langSel").removeClass("on");
    if (lang == "en") $("#langSelEn").addClass("on");
    else if (lang == "hu") $("#langSelHu").addClass("on");
  }

  return {
    // Called by page-specific controller scripts to register themselves in single-page app, when page is navigated to.
    registerInitScript: function(pageRel, init) {
      initScripts[pageRel] = init;
    },

    globalInit: function(init) {
      globalInitScripts.push(init);
    },

    getLang: function() {
      return lang;
    },

    isMobile: function() {
      return false;
    },

    submitSearch: function(query) {
      history.pushState(null, null, "/" + lang + "/search/" + query);
      dynNavigate();
    },

    // Gets the current selection's bounding element (start), or null if page has no selection.
    getSelBoundElm: function () {
      var range, sel, container;
      if (document.selection) {
        range = document.selection.createRange();
        range.collapse(true);
        if (range.toString() == "") return null;
        return range.parentElement();
      } else {
        sel = window.getSelection();
        if (sel.toString() == "") return null;
        if (sel.getRangeAt) {
          if (sel.rangeCount > 0) {
            range = sel.getRangeAt(0);
          }
        } else {
          // Old WebKit
          range = document.createRange();
          range.setStart(sel.anchorNode, sel.anchorOffset);
          range.setEnd(sel.focusNode, sel.focusOffset);

          // Handle the case when the selection was selected backwards (from the end to the start in the document)
          if (range.collapsed !== sel.isCollapsed) {
            range.setStart(sel.focusNode, sel.focusOffset);
            range.setEnd(sel.anchorNode, sel.anchorOffset);
          }
        }
        if (range) {
          container = range["startContainer"];
          // Check if the container is a text node and return its parent if so
          return container.nodeType === 3 ? container.parentNode : container;
        }
      }
    },

    // Called by any code showing a modal popup. Closes any active popup, and remembers close function.
    modalShown: function (closeFun) {
      if (activeModalCloser == closeFun) return;
      if (activeModalCloser != null) activeModalCloser();
      activeModalCloser = closeFun;
    },

    modalHidden: function() {
      activeModalCloser = null;
    },

    // Shows an alert at the top of the page.
    showAlert: function (title, body, isError) {
      // Remove old alert
      $(".alertBar").remove();
      // Class for current one
      ++alertId;
      var currBarId = "alertbar" + alertId;
      var currAlertId = "alert" + alertId;
      var templ = alertTemplate.replace("alertBarId", currBarId);
      templ = templ.replace("alertId", currAlertId);
      var elm = $(templ);
      $("body").append(elm);
      $("#" + currAlertId + " .alertTitle").text(title);
      if (body) {
        $("#" + currAlertId + " .alertTitle").append("<br>");
        $("#" + currAlertId + " .alertBody").text(body);
      }
      if (isError) $("#" + currAlertId).addClass("alertFail");
      else $("#" + currAlertId).addClass("alertOK");

      $("#" + currAlertId + " .alertClose").click(function () {
        $("#" + currBarId).remove();
        //$("#" + currAlertId).addClass("hidden");
        //setTimeout(function () {
        //  $("#" + currBarId).remove();
        //}, 5000);
      });

      setTimeout(function () {
        $("#" + currAlertId).addClass("visible");
        setTimeout(function () {
          $("#" + currAlertId).addClass("hidden");
          setTimeout(function () {
            $("#" + currBarId).remove();
          }, 1000)
        }, 5000);
      }, 50);
    }

  };

})();
