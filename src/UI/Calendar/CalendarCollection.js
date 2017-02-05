var Backbone = require('backbone');
var EventModel = require('./EventModel');

module.exports = Backbone.Collection.extend({
    url       : window.NzbDrone.ApiRoot + '/calendar',
    model     : EventModel,
    tableName : 'calendar',

    comparator : function(model) {
        var date = new Date(model.get('availableFrom'));
        var time = date.getTime();
        return time;
    }
});
