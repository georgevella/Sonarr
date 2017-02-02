var Marionette = require('marionette');

module.exports = Marionette.ItemView.extend({
    template : 'Shared/NotFoundViewTemplate',

    onRender : function(){
        console.info("rendering shared/notfound");
    }
});