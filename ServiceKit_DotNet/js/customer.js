/* -----------------------------------------------------------------------------------------------
 * Customer Scripts
 * -----------------------------------------------------------------------------------------------*/
/* global jQuery, _, EventEmitter2, setImmediate, OT */

// Prevent leaking into global scope
!(function(exports, doc, $, _, EventEmitter, setImmediate, presentAlert, validateForm, ping, OT,
           undefined) {

  // State
  var servicePanel,
      $serviceRequestButton;

  // Service Request
  //
  // This object controls the interaction with the modal and its contained form that the user must
  // fill before speaking to a representative.
  //
  // Using a module pattern to encapsulate the functionality. The object returned at the end of this
  // function presents the API for using this module. It is a singleton and can be reused.
  var serviceRequest = (function() {
    var $modal, $form, $fields, sessionDataCallback;

    var init = function(modalSelector, callback) {
      $modal = $(modalSelector);
      $form = $modal.find('.request-form');
      $fields = $form.find('input, textarea');
      sessionDataCallback = callback;

      $form.submit(submit);
      $modal.find('.request-submit').click(function() {
        $form.submit();
      });
      $modal.on('hidden.bs.modal', modalHidden);
    };

    var submit = function(event) {
      var requestData = $fields.serialize();
      event.preventDefault();

      disableFields();

      if (validateForm($form, validationRequirements) === false) {
        enableFields();
        return;
      }

      $.post('/help/session', requestData, 'json')
        .done(function(data) {
          sessionDataCallback({
            apiKey: data.apiKey,
            sessionId: data.sessionId,
            token: data.token
          });
          $modal.modal('hide');
        })
        .fail(function() {
          presentAlert('Request failed. Try again later.', 'danger', $form.parent(), false);
        })
        .always(function() {
          enableFields();
        });
    };

    var modalHidden = function() {
      $form[0].reset();
    };

    var validationRequirements = {
      '.customer-name': {
        maxLength: 50,
        required: true
      },
      '.problem-text': {
        maxLength: 200,
        required: true
      }
    };

    var disableFields = function() {
      $fields.prop('disabled', true);
    };

    var enableFields = function() {
      $fields.prop('disabled', false);
    };

    return {
      init: init
    };
  }());


  // Service Panel
  //
  // This object controls the interaction with a floating panel which is fixed to the bottom right
  // of the page, where a conversation with the representative is contained.
  var ServicePanel = function (selector, sessionData) {
    EventEmitter.call(this);

    this.apiKey = sessionData.apiKey;
    this.sessionId = sessionData.sessionId;
    this.token = sessionData.token;
    this.connected = false;

    this.$panel = $(selector);
    this.$publisher = this.$panel.find('.publisher');
    this.$subscriber = this.$panel.find('.subscriber');
    this.$waitingHardwareAccess = this.$panel.find('.waiting .hardware-access');
    this.$waitingRepresentative = this.$panel.find('.waiting .representative');
    this.$closeButton = this.$panel.find('.close-button');

    // Do this asynchronously so that the 'open' event happens on a separate turn of the event loop
    // than the instantiation.
    setImmediate(this.initialize.bind(this));
    ping(this.apiKey);
  };
  ServicePanel.prototype = new EventEmitter();

  // Start the UI and communications
  ServicePanel.prototype.initialize = function() {
    this.session = OT.initSession(this.apiKey, this.sessionId);
    this.session.on('sessionConnected', this._sessionConnected, this)
                .on('sessionDisconnected', this._sessionDisconnected, this)
                .on('streamCreated', this._streamCreated, this)
                .on('streamDestroyed', this._streamDestroyed, this);

    this.publisher = OT.initPublisher(this.$publisher[0], this._videoProperties);
    this.publisher.on('accessAllowed', this._publisherAllowed, this)
                  .on('accessDenied', this._publisherDenied, this);

    this.$closeButton.on('click', this.close.bind(this));
    this.$panel.show();
    this.$waitingHardwareAccess.show();

    this.emit('open');
  };

  ServicePanel.prototype.close = function() {
    if (this.connected) {
      this.session.disconnect();
    } else {
      this._cleanUp();
    }
  };

  ServicePanel.prototype._cleanUp = function() {
    this.$waitingHardwareAccess.hide();
    this.$waitingRepresentative.hide();
      console.log("getPricing just got called with");
   
    this.$closeButton.off().text('Cancel');

    this.session.off();
    this.publisher.off();

    if (this.queueId) {
      this._dequeue();
    }

    this.$panel.hide();
    this.emit('close');
  };

  ServicePanel.prototype._dequeue = function () {
    //  console.log("getPricing just got called with");

    //$.ajax({
    //  type: 'POST',
    //  url: '/help/deleteq/'+this.queueId,
    //  data: "",
    //  async: false
    //})
    //.done(function(data) {
    //  console.log(data);
    //})
    //.always(function() {
    //  console.log('dequeue request completed');
    //});
      //exports.onbeforeunload = undefined;

      $.post('/help/deleteq', { 'session_id': this.queueId }, 'json')
     .done(function(data) {
        // self.queueId = data.queueId;

         // install a synchronous http request to remove from queue in case the page is closed
         exports.onbeforeunload = self._dequeue.bind(self);
     })
     .fail(function() {
         //presentAlert('Could not join the queue at this time. Try again later.', 'warning');
         self.close();
     });
  };




 

  // Waits to connect to the session until after the user approves access to camera and mic
  ServicePanel.prototype._publisherAllowed = function() {
    this.$waitingHardwareAccess.hide();
    this.$waitingRepresentative.show();
    this.session.connect(this.token, function(err) {
      // Handle connect failed
      if (err && err.code === 1006) {
        console.log('Connecting to the session failed. Try connecting to this session again.');
      }
    });
  };

  ServicePanel.prototype._publisherDenied = function() {
    presentAlert('Camera access denied. Please reset the setting in your browser and try again.',
                 'danger');
    this.close();
  };

  ServicePanel.prototype._sessionConnected = function() {
    var self = this;

    this.connected = true;
    this.session.publish(this.publisher, function(err) {
      // Handle publisher failing to connect
      if (err && err.code === 1013) {
        console.log('The publisher failed to connect.');
        self.close();
      }
    });

    // Once the camera and mic are accessible and the session is connected, join the queue
    $.post('/help/queue', { 'session_id' : this.sessionId }, 'json')
      .done(function(data) {
        self.queueId = data.queueId;

        // install a synchronous http request to remove from queue in case the page is closed
        exports.onbeforeunload = self._dequeue.bind(self);
      })
      .fail(function() {
        presentAlert('Could not join the queue at this time. Try again later.', 'warning');
        self.close();
      });
  };

  ServicePanel.prototype._sessionDisconnected = function() {
    this.connected = false;
    this._cleanUp();
  };

  ServicePanel.prototype._streamCreated = function(event) {
    // The representative joins the session
    if (!this.subscriber) {
      this.subscriber = this.session.subscribe(event.stream,
                                               this.$subscriber[0],
                                               this._videoProperties,
                                               function(err) {
        // Handle subscriber error
        if (err && err.code === 1600) {
          console.log('An internal error occurred. Try subscribing to this stream again.');
        }
      });
      this.$closeButton.text('End');
      this.$waitingRepresentative.hide();

      // Invalidate queueId because if the representative arrived, that means customer has been
      // dequeued
      this.queueId = undefined;
      exports.onbeforeunload = undefined;
    }
  };

  ServicePanel.prototype._streamDestroyed = function(event) {
    // If the representative leaves, the call is done
    if (this.subscriber && event.stream === this.subscriber.stream) {
      this.close();
    }
  };

  ServicePanel.prototype._videoProperties = {
    insertMode: 'append',
    width: '100%',
    height: '100%',
    style: {
      buttonDisplayMode: 'off'
    }
  };



  // Page level interactions
  //
  // Hooks up the button on the page to interacting with the Service Request as well as initializing
  // and tearing down a Service Panel instance
  $(doc).ready(function() {
    $serviceRequestButton = $('.service-request-btn');

    serviceRequest.init('#service-request-modal', function(serviceSessionData) {
      // Initialize a Service Panel instance
      servicePanel = new ServicePanel('#service-panel', serviceSessionData);

      // Make sure the user cannot attempt to open another Service Panel
      servicePanel.on('open', disableServiceRequest);

      // Make sure that the instance gets torn down and UI is renabled when the Service Panel is
      // closed
      servicePanel.on('close', function() {
        enableServiceRequest();
        servicePanel.removeAllListeners();
        servicePanel = undefined;
      });
    });
  });

  // Page level helper methods

  var disableServiceRequest = function() {
    $serviceRequestButton.prop('disabled', true);
  };

  var enableServiceRequest = function() {
    $serviceRequestButton.prop('disabled', false);
  };


}(window, window.document, jQuery, _, EventEmitter2, setImmediate, window.presentAlert,
  window.validateForm, window.ping, OT));
