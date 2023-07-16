// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(function () {
    $(".sl2").select2({
        theme: "bootstrap-5",
    });

    $('#ExcludedDatePicker').datepicker({
        multidate: true,
        language: 'fr',
        clearBtn: true,
    }).datepicker('setDates',
        $('#ExcludedDates').val().split(',').map(
            function (s) {
                return new Date(s);
            })
    ).on('change', function () {
        var formatDateYYYYMMDD = function (d) { return d.getFullYear() + '-' + ('00' + (d.getMonth() + 1)).slice(-2) + '-' + ('00' + d.getDate()).slice(-2); };
        $('#ExcludedDates').val(
            $('#ExcludedDatePicker').datepicker('getDates')
                .map(formatDateYYYYMMDD)
                .sort()
                .join(","));
    });

    $('#Country').on('change', function () { this.form.submit(); });

    var updatePayRate = function () {
        $('#payrateInfo').html(
            $('#PayRate').val()
        );
    };
    $('#PayRate').on('change', updatePayRate);
    updatePayRate();

    $('.resetpayrate').on('change', function () {
        $('#PayRate').val('0');
        updatePayRate();
    });

    var updateWorkHours = function () {
        var hours = $('.workhours').toArray().reduce(function (tot, e) { return tot + parseInt($(e).val()); }, 0);
        var totalMinutes = $('.workminutes').toArray().reduce(function (tot, e) { return tot + parseInt($(e).val()); }, 0)
            + hours * 60;

        hours = Math.floor(totalMinutes / 60);
        var minutes = totalMinutes % 60;

        $('#hoursInfo').html(
            hours + 'h' + (minutes > 0 ? minutes : '')
        );
    };
    $('.workhours').on('change', updateWorkHours);
    $('.workminutes').on('change', updateWorkHours);
    updateWorkHours();

    $('.explaination a').attr('target', 'blank');

    $('form input,form select').on('change', function () {
        $('#refresh-needed').show();
        $('#all-results').hide();
    })
});